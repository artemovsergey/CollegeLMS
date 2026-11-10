#!/usr/bin/env node
/**
 * DSH Session Analytics — разбор сессий DeepSeek Harness.
 *
 * Что считается по каждой сессии:
 *   - модели (provider/model) и токены по каждой модели: вход без кэша,
 *     кэш-чтение, выход, reasoning, всего;
 *   - ходы, шаги, длительность, ошибки, повторы LLM-запросов;
 *   - инструменты, сгруппированные по юнитам DSH (файлы, PowerShell,
 *     субагенты, расписание, плагины, workflow, present, web, skill и т.д.);
 *   - загруженные скиллы (tool `skill`) и косвенные чтения SKILL.md;
 *   - MCP: какие серверы были доступны в сессии и какие реально вызывались;
 *   - операции `plugin_manager` (install/remove/list) с результатом;
 *   - субагенты: метки, модели, дочерние сессии;
 *   - профиль desktop: бандлы, вставки конфига, MCP-серверы.
 *
 * Данные: ~/.dsh/sessions/<workspace>/<session>/session.v4.jsonl.zstd —
 * поток zstd-кадров, по одному кадру на событие. Node не умеет читать
 * склеенные кадры одним вызовом, поэтому кадры режутся по magic-байтам
 * и распаковываются по отдельности.
 *
 * Результат:
 *   ~/.dsh/session-analytics/latest.md            — сводный отчёт;
 *   ~/.dsh/session-analytics/reports/YYYY-MM-DD.md — отчёт за день;
 *   ~/.dsh/session-analytics/data/summary.json     — машиночитаемая сводка;
 *   ~/.dsh/session-analytics/data/sessions/<id>.json — метрики сессии;
 *   ~/.dsh/session-analytics/data/state.json       — инкрементальный кеш.
 *
 * Запуск:
 *   node scripts/dsh/session-analytics.mjs            # инкрементально + отчёт
 *   node scripts/dsh/session-analytics.mjs --reindex  # пересобрать всё
 *   node scripts/dsh/session-analytics.mjs --session=<id|префикс>
 *   node scripts/dsh/session-analytics.mjs --since=7d
 *   node scripts/dsh/session-analytics.mjs --watch
 *   node scripts/dsh/session-analytics.mjs --json | --quiet
 */

import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import zlib from 'node:zlib';
import crypto from 'node:crypto';

const HOME = process.env.USERPROFILE || os.homedir();
const DSH_HOME = path.join(HOME, '.dsh');
const SESSIONS_ROOT = path.join(DSH_HOME, 'sessions');
const PROFILE_DIR = path.join(DSH_HOME, 'profiles', 'desktop');
const ROOT = path.join(DSH_HOME, 'session-analytics');
const DATA_DIR = path.join(ROOT, 'data');
const SESSION_DATA_DIR = path.join(DATA_DIR, 'sessions');
const REPORTS_DIR = path.join(ROOT, 'reports');
const STATE_PATH = path.join(DATA_DIR, 'state.json');
const SUMMARY_PATH = path.join(DATA_DIR, 'summary.json');
const LATEST_PATH = path.join(ROOT, 'latest.md');
const LOCK_PATH = path.join(DATA_DIR, '.lock');
const LOCK_TTL_MS = 10 * 60 * 1000;
const STATE_VERSION = 1;
const ANALYZER_VERSION = 5;
const ZSTD_MAGIC = Buffer.from([0x28, 0xb5, 0x2f, 0xfd]);

if (typeof zlib.zstdDecompressSync !== 'function') {
  console.error(
    `[session-analytics] нужен Node.js 22.15+ с поддержкой zstd; сейчас ${process.version}. ` +
      'Запустите через run.cmd — он берёт Node из рантайма DSH.',
  );
  process.exit(1);
}

const nf = new Intl.NumberFormat('ru-RU');
const num = (n) => nf.format(Math.round(Number(n) || 0));

/* ────────────────────────────── юниты DSH ────────────────────────────── */

// Инструмент → юнит, который его предоставляет. Набор соответствует
// @deepseek-ai/dsh-tool-* в установленном DSH 0.2.2-rc.1.
const TOOL_UNITS = {
  read: 'files',
  write: 'files',
  edit: 'files',
  read_image: 'files',
  glob: 'search',
  grep: 'search',
  pwsh: 'shell',
  bash: 'shell',
  subagent: 'subagents',
  subagent_fork: 'subagents',
  list_subagent_models: 'subagents',
  list_agents: 'subagents',
  send_message: 'subagents',
  interrupt_agent: 'subagents',
  schedule_create: 'schedule',
  schedule_edit: 'schedule',
  schedule_list: 'schedule',
  schedule_view: 'schedule',
  plugin_manager: 'plugins',
  skill: 'skills',
  create_goal: 'planning',
  get_goal: 'planning',
  update_goal: 'planning',
  todo_write: 'planning',
  job_kill: 'jobs',
  job_list: 'jobs',
  job_output: 'jobs',
  present: 'deliverables',
  web_fetch: 'web',
  web_search: 'web',
  workflow: 'workflow',
  ask_user_question: 'dialog',
  load_workspace_dependencies: 'workspace',
  working_directory: 'workspace',
  list_mcp_resources: 'mcp-resources',
  list_mcp_resource_templates: 'mcp-resources',
  read_mcp_resource: 'mcp-resources',
};

const UNIT_LABELS = {
  files: 'Файлы',
  search: 'Поиск',
  shell: 'PowerShell',
  subagents: 'Субагенты',
  schedule: 'Расписание',
  plugins: 'Менеджер плагинов',
  skills: 'Скиллы',
  planning: 'Цели и планы',
  jobs: 'Фоновые задачи',
  deliverables: 'Документы (present)',
  web: 'Веб-инструменты',
  workflow: 'Workflow',
  dialog: 'Вопросы пользователю',
  workspace: 'Окружение',
  'mcp-resources': 'MCP-ресурсы',
  other: 'Прочее',
};

// Юниты, которые интересны как «плагины/расширения», а не базовые
// файловые операции.
const EXTENSION_UNITS = new Set([
  'subagents',
  'schedule',
  'plugins',
  'skills',
  'planning',
  'jobs',
  'deliverables',
  'web',
  'workflow',
  'dialog',
  'workspace',
  'mcp-resources',
]);

const STATUS = {
  active: { icon: '●', label: 'идёт' },
  done: { icon: '✓', label: 'завершена' },
  error: { icon: '✕', label: 'ошибка' },
  interrupted: { icon: '⏸', label: 'прервана' },
  empty: { icon: '·', label: 'пустая' },
};

/* ─────────────────────────────── утилиты ─────────────────────────────── */

function pad(n) {
  return String(n).padStart(2, '0');
}

function fmtDateTime(ts) {
  if (!ts) return '—';
  const d = new Date(ts);
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fmtDayTime(ts) {
  if (!ts) return '—';
  const d = new Date(ts);
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fmtDuration(ms) {
  if (!Number.isFinite(ms) || ms <= 0) return '—';
  const minutes = Math.round(ms / 60_000);
  if (minutes < 1) return '<1 мин';
  if (minutes < 60) return `${minutes} мин`;
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  return `${h} ч ${pad(m)} мин`;
}

function todayISO() {
  const d = new Date();
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

function oneLine(text) {
  return String(text ?? '')
    .replace(/\s+/g, ' ')
    .trim();
}

function truncate(text, max = 60) {
  const clean = oneLine(text);
  if (clean.length <= max) return clean;
  const cut = clean.slice(0, max - 1);
  const atWord = cut.replace(/\s+\S*$/, '');
  return `${(atWord || cut).trimEnd()}…`;
}

function mdCell(text) {
  return oneLine(text).replace(/\|/g, '\\|') || '—';
}

function shortId(id) {
  return String(id || '').replace(/^session-/, '').slice(0, 8);
}

function parseSince(value) {
  if (!value) return null;
  const m = /^(\d+)\s*([dhm])$/i.exec(value.trim());
  if (m) {
    const amount = Number(m[1]);
    const unit = m[2].toLowerCase();
    const ms = unit === 'd' ? 86_400_000 : unit === 'h' ? 3_600_000 : 60_000;
    return Date.now() - amount * ms;
  }
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function readJson(file, fallback) {
  try {
    return fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : fallback;
  } catch {
    return fallback;
  }
}

function writeAtomic(file, content) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  const tmp = `${file}.${process.pid}.tmp`;
  fs.writeFileSync(tmp, content, 'utf8');
  try {
    fs.renameSync(tmp, file);
  } catch {
    fs.rmSync(file, { force: true });
    fs.renameSync(tmp, file);
  }
}

function ensureDirs() {
  for (const dir of [ROOT, DATA_DIR, SESSION_DATA_DIR, REPORTS_DIR]) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

function emptyTokens() {
  return {
    input: 0,
    output: 0,
    cacheRead: 0,
    cacheWrite: 0,
    reasoning: 0,
    total: 0,
  };
}

function addTokens(target, source) {
  target.input += source.input || 0;
  target.output += source.output || 0;
  target.cacheRead += source.cacheRead || 0;
  target.cacheWrite += source.cacheWrite || 0;
  target.reasoning += source.reasoning || 0;
  target.total += source.total || 0;
  return target;
}

function addUsage(target, usage) {
  const input = usage.inputTokens || 0;
  const output = usage.outputTokens || 0;
  const cacheRead = usage.cacheReadTokens || 0;
  const cacheWrite = usage.cacheWriteTokens || 0;
  const reasoning = usage.reasoningTokens || 0;
  const total = usage.totalTokens || input + output + cacheRead + cacheWrite;
  target.input += input;
  target.output += output;
  target.cacheRead += cacheRead;
  target.cacheWrite += cacheWrite;
  target.reasoning += reasoning;
  target.total += total;
  return target;
}

function tokenSummary(tokens) {
  const parts = [
    `вход ${num(tokens.input)}`,
    `кэш ${num(tokens.cacheRead)}`,
    `выход ${num(tokens.output)}`,
  ];
  if (tokens.reasoning) parts.push(`reasoning ${num(tokens.reasoning)}`);
  if (tokens.cacheWrite) parts.push(`кэш-запись ${num(tokens.cacheWrite)}`);
  return parts.join(' · ');
}

/* ────────────────────────────── профиль ─────────────────────────────── */

function readDshVersion() {
  try {
    const runtime = path.join(DSH_HOME, 'dsh-runtimes', 'dsh-primary-runtime', 'runtime.json');
    return JSON.parse(fs.readFileSync(runtime, 'utf8')).desktopVersion || null;
  } catch {
    return null;
  }
}

function readProfile() {
  const info = {
    bundles: [],
    patchEntries: [],
    mcpServers: [],
    packages: [],
    version: readDshVersion(),
  };

  try {
    const pkg = JSON.parse(fs.readFileSync(path.join(PROFILE_DIR, 'package.json'), 'utf8'));
    info.bundles = pkg?.dsh?.profile?.bundles ?? [];
  } catch {
    /* профиль может быть ещё не создан */
  }

  try {
    const yml = fs.readFileSync(path.join(PROFILE_DIR, 'cordis.patch.yml'), 'utf8');
    info.patchEntries = [...yml.matchAll(/^\s*-\s+id:\s*([^\s#]+)/gm)].map((m) => m[1]);
    info.mcpServers = [...yml.matchAll(/^\s*serverName:\s*([^\s#]+)/gm)].map((m) => m[1]);
  } catch {
    /* конфиг может отсутствовать */
  }

  try {
    const nodeModules = path.join(PROFILE_DIR, 'node_modules');
    for (const name of fs.readdirSync(nodeModules)) {
      if (name.startsWith('.')) continue;
      const pkgJson = path.join(nodeModules, name, 'package.json');
      if (!fs.existsSync(pkgJson)) continue;
      try {
        const pkg = JSON.parse(fs.readFileSync(pkgJson, 'utf8'));
        info.packages.push(pkg.name || name);
      } catch {
        info.packages.push(name);
      }
    }
  } catch {
    /* node_modules может не быть */
  }

  info.patchEntries = [...new Set(info.patchEntries)];
  info.mcpServers = [...new Set(info.mcpServers)];
  info.packages = [...new Set(info.packages)];
  return info;
}

/* ───────────────────────────── zstd-кадры ───────────────────────────── */

/**
 * Распаковывает поток zstd-кадров. Кадры ищутся по magic-байтам: DSH пишет
 * по одному кадру на событие, а Node не поддерживает склейку кадров в
 * zstdDecompressSync.
 */
function decodeZstd(buf) {
  const offsets = [];
  let cursor = 0;
  while ((cursor = buf.indexOf(ZSTD_MAGIC, cursor)) !== -1) {
    offsets.push(cursor);
    cursor += ZSTD_MAGIC.length;
  }

  if (offsets.length === 0) {
    return { text: '', frames: 0, badFrames: 0, truncated: false };
  }

  let text = '';
  let badFrames = 0;
  let truncated = false;

  for (let i = 0; i < offsets.length; i += 1) {
    const start = offsets[i];
    const end = i + 1 < offsets.length ? offsets[i + 1] : buf.length;
    try {
      text += zlib.zstdDecompressSync(buf.subarray(start, end)).toString('utf8');
      continue;
    } catch {
      // Последний кадр мог быть недописан в момент чтения.
      if (i === offsets.length - 1) {
        truncated = true;
        continue;
      }
      // Редкий случай: magic-байты встретились внутри сжатых данных.
      // Пробуем склеить с следующим кадром.
      let merged = false;
      for (let j = i + 2; j <= offsets.length; j += 1) {
        const mergedEnd = j < offsets.length ? offsets[j] : buf.length;
        try {
          text += zlib.zstdDecompressSync(buf.subarray(start, mergedEnd)).toString('utf8');
          merged = true;
          i = j - 1;
          break;
        } catch {
          /* пробуем следующий кадр */
        }
      }
      if (!merged) badFrames += 1;
    }
  }

  return { text, frames: offsets.length, badFrames, truncated };
}

/* ─────────────────────────── анализ сессии ──────────────────────────── */

function normalizeSessionText(text) {
  // Заголовок сессии, её название и маркер форка содержат идентификатор/номер
  // копии, поэтому в отпечаток и сравнение копий они не входят.
  return text
    .split('\n')
    .filter(
      (line) =>
        line.trim() &&
        !line.startsWith('{"type":"session"') &&
        !line.startsWith('{"type":"session/title"') &&
        !line.startsWith('{"type":"session/end-seed"'),
    )
    .join('\n');
}

function readNormalizedSession(file) {
  return normalizeSessionText(decodeZstd(fs.readFileSync(file)).text);
}

function parseToolArgs(raw) {
  if (!raw) return {};
  if (typeof raw === 'object') return raw;
  try {
    return JSON.parse(raw);
  } catch {
    return {};
  }
}

function messageText(content) {
  if (typeof content === 'string') return content;
  if (!Array.isArray(content)) return '';
  return content
    .filter((part) => part && part.type === 'text' && typeof part.text === 'string')
    .map((part) => part.text)
    .join('\n');
}

function unitOfTool(name) {
  if (name.startsWith('mcp__')) {
    const match = /^mcp__(.+?)__/.exec(name);
    return match ? `mcp:${match[1]}` : 'mcp-resources';
  }
  return TOOL_UNITS[name] || 'other';
}

function analyzeSession(file, stat) {
  const decoded = decodeZstd(fs.readFileSync(file));
  const events = [];
  for (const line of decoded.text.split('\n')) {
    if (!line.trim()) continue;
    try {
      events.push(JSON.parse(line));
    } catch {
      /* битая строка — пропускаем */
    }
  }

  const session = {
    id: null,
    file,
    parentId: null,
    origin: null,
    depth: 0,
    isSeeded: false,
    cwd: null,
    project: '—',
    createdAt: null,
    updatedAt: null,
    firstEventAt: null,
    lastEventAt: null,
    title: '',
    titleSource: null,
    agentPreset: null,
    permissions: {},
    turns: 0,
    steps: 0,
    messages: 0,
    models: [],
    tokens: emptyTokens(),
    toolCounts: {},
    units: {},
    skills: {},
    skillReads: {},
    mcpAvailable: {},
    mcpUsed: {},
    pluginOps: [],
    subagents: [],
    errors: [],
    prompts: [],
    modelSwitches: [],
    lastTurnReason: null,
    openTurn: false,
    toolErrors: 0,
    approvals: 0,
    commands: 0,
    jobs: 0,
    toolTimeMs: 0,
    deliverables: 0,
    todos: 0,
    goals: 0,
    workspaceChanges: 0,
    titleCalls: 0,
    retries: 0,
    contextWindow: null,
    availableTools: 0,
    status: 'empty',
    durationMs: 0,
    spanMs: 0,
    filesRead: 0,
    filesWritten: 0,
    filesEdited: 0,
    fingerprint: null,
    normalizedLength: 0,
    normalizedHeadHash: null,
    sizeBytes: stat?.size ?? 0,
    mtimeMs: stat?.mtimeMs ?? 0,
    truncated: decoded.truncated,
    badFrames: decoded.badFrames,
  };

  // Отпечаток содержимого без заголовка и заголовков сессии: так находятся
  // точные копии сессий (DSH умеет создавать дубли с суффиксом «(2)»).
  const normalized = normalizeSessionText(decoded.text);
  session.fingerprint = crypto.createHash('sha256').update(normalized).digest('hex').slice(0, 16);
  session.normalizedLength = normalized.length;
  session.normalizedHeadHash = crypto
    .createHash('sha256')
    .update(normalized.slice(0, 8192))
    .digest('hex')
    .slice(0, 16);

  const modelBuckets = new Map();
  const stepSet = new Set();
  const callIndex = new Map();
  const descriptors = [];
  const catalogs = [];
  let currentModel = { provider: null, model: null };
  let lastTurnStart = null;

  for (const event of events) {
    const type = event.type;
    const data = event.data || {};
    const time = event.time || 0;

    if (time) {
      if (!session.firstEventAt || time < session.firstEventAt) session.firstEventAt = time;
      if (!session.lastEventAt || time > session.lastEventAt) session.lastEventAt = time;
    }

    switch (type) {
      case 'session': {
        // Заголовок сессии лежит в самом событии, без поля data.
        session.id = event.id || session.id;
        session.parentId = event.parentSession || null;
        session.origin = event.origin || (event.delegationDepth ? 'subagent' : 'user');
        session.depth = event.delegationDepth || 0;
        session.isSeeded = Boolean(event.isSeeded);
        session.cwd = event.cwd || session.cwd;
        session.createdAt = event.createdAt || session.createdAt;
        session.agentPreset = event.agentPreset || session.agentPreset;
        break;
      }
      case 'permission/preset':
        session.permissions.preset = data.preset;
        break;
      case 'sandbox/mode':
        session.permissions.sandbox = data.mode;
        break;
      case 'approval/policy':
        session.permissions.approval = data.policy;
        break;
      case 'agent-preset/selected':
        session.agentPreset = data.preset || data.agentPreset || session.agentPreset;
        break;
      case 'request/header': {
        const config = data.header?.config;
        if (config?.provider || config?.model) {
          currentModel = { provider: config.provider, model: config.model };
        }
        const tools = data.header?.tools;
        if (Array.isArray(tools)) {
          session.availableTools = tools.length;
          const available = {};
          for (const tool of tools) {
            const name = typeof tool === 'string' ? tool : tool?.name;
            if (!name) continue;
            const match = /^mcp__(.+?)__(.+)$/.exec(name);
            if (!match) continue;
            if (!available[match[1]]) available[match[1]] = new Set();
            available[match[1]].add(match[2]);
          }
          session.mcpAvailable = available;
        }
        break;
      }
      case 'request/context':
        if (data.contextWindow) session.contextWindow = data.contextWindow;
        if (data.provider || data.model) {
          currentModel = { provider: data.provider, model: data.model };
        }
        break;
      case 'model/selection':
        if (data.provider || data.model) {
          currentModel = { provider: data.provider, model: data.model };
          session.modelSwitches.push({
            provider: data.provider,
            model: data.model,
            time,
          });
        }
        break;
      case 'user/message': {
        const text = oneLine(messageText(data.content));
        if (text && session.prompts.length < 5) {
          session.prompts.push(truncate(text, 240));
        }
        break;
      }
      case 'turn/start':
        session.turns += 1;
        session.openTurn = true;
        lastTurnStart = time;
        break;
      case 'turn/end': {
        session.openTurn = false;
        const reason = data.reason || {};
        session.lastTurnReason = {
          kind: reason.kind || null,
          message: reason.error?.message || null,
        };
        if (reason.kind === 'error') {
          session.errors.push({
            turn: data.turn,
            message: truncate(reason.error?.message || 'ошибка', 200),
          });
        }
        if (lastTurnStart && time) {
          session.durationMs += Math.max(0, time - lastTurnStart);
        }
        lastTurnStart = null;
        break;
      }
      case 'step/start':
        stepSet.add(`${data.turn}.${data.step}`);
        break;
      case 'assistant/message': {
        session.messages += 1;
        const usage = data.usage || {};
        const source = data.message?.source || {};
        const provider = source.provider || currentModel.provider || '?';
        const model = source.model || currentModel.model || '?';
        const key = `${provider}/${model}`;
        let bucket = modelBuckets.get(key);
        if (!bucket) {
          bucket = {
            provider,
            model,
            messages: 0,
            tokens: emptyTokens(),
          };
          modelBuckets.set(key, bucket);
        }
        bucket.messages += 1;
        addUsage(bucket.tokens, usage);
        addUsage(session.tokens, usage);
        break;
      }
      case 'tool/call': {
        const name = data.name || '?';
        session.toolCounts[name] = (session.toolCounts[name] || 0) + 1;
        const args = parseToolArgs(data.arguments);

        if (name === 'skill' && args.name) {
          session.skills[args.name] = (session.skills[args.name] || 0) + 1;
        } else if (name === 'read' && typeof args.file_path === 'string') {
          const match = /[\\/]skills[\\/]([^\\/]+)[\\/][^\\/]*SKILL\.md$/i.exec(args.file_path);
          if (match) {
            session.skillReads[match[1]] = (session.skillReads[match[1]] || 0) + 1;
          }
        } else if (name === 'plugin_manager') {
          const op = {
            action: args.action || '?',
            target: truncate(args.target || args.name || args.package || '', 60) || '—',
            callId: data.callId || null,
            time,
            ok: null,
          };
          session.pluginOps.push(op);
          if (op.callId) callIndex.set(op.callId, op);
        }

        if (name === 'read') session.filesRead = (session.filesRead || 0) + 1;
        if (name === 'write') session.filesWritten = (session.filesWritten || 0) + 1;
        if (name === 'edit') session.filesEdited = (session.filesEdited || 0) + 1;
        if (name === 'pwsh' || name === 'bash') session.commands += 1;

        const mcp = /^mcp__(.+?)__(.+)$/.exec(name);
        if (mcp) {
          session.mcpUsed[mcp[1]] = (session.mcpUsed[mcp[1]] || 0) + 1;
        }
        break;
      }
      case 'tool/result': {
        // В DSH v4 результат инструмента лежит в data.message.
        const payload = data.message || data;
        if (payload.isError) session.toolErrors += 1;
        const callId = payload.toolCallId || data.toolCallId;
        if (callId && callIndex.has(callId)) {
          callIndex.get(callId).ok = !payload.isError;
        }
        break;
      }
      case 'job/registered':
        session.jobs += 1;
        break;
      case 'job/settled':
        if (data.startedAt && data.finishedAt) {
          session.toolTimeMs += Math.max(0, data.finishedAt - data.startedAt);
        }
        break;
      case 'subagent/descriptor':
        // В дочерней сессии первым событием идёт её собственный дескриптор;
        // дескрипторы реальных субагентов появляются уже после turn/start.
        if (session.turns === 0) break;
        descriptors.push({
          label: data.label || 'субагент',
          provider: data.agentProvider || null,
          model: data.agentModel || null,
          reasoningEffort: data.agentReasoningEffort || null,
          mode: data.mode || null,
        });
        break;
      case 'subagent/catalog':
        catalogs.push({
          childId: data.childId || null,
          label: data.label || null,
          mode: data.mode || null,
        });
        break;
      case 'session/title':
        if (data.title) {
          session.title = oneLine(data.title);
          session.titleSource = data.source?.kind || null;
        }
        break;
      case 'session/title-llm-request':
        session.titleCalls += 1;
        break;
      case 'llm/retry':
        session.retries += 1;
        break;
      case 'approval/asked':
        session.approvals += 1;
        break;
      case 'command/run':
        if (data.name === 'permission') session.approvals += 1;
        break;
      case 'goal/change':
        session.goals += 1;
        break;
      case 'todo/write':
        session.todos += 1;
        break;
      case 'deliverables/presented':
        session.deliverables += 1;
        break;
      case 'workspace/changes':
        session.workspaceChanges += 1;
        break;
      default:
        break;
    }
  }

  if (!session.id) session.id = path.basename(path.dirname(file));
  if (!session.createdAt) session.createdAt = session.firstEventAt || session.mtimeMs || 0;
  if (!session.updatedAt) session.updatedAt = session.lastEventAt || session.mtimeMs || 0;
  if (!session.cwd) session.cwd = '—';
  session.project = session.cwd && session.cwd !== '—' ? path.basename(session.cwd) : '—';
  session.steps = stepSet.size;
  session.spanMs = Math.max(0, session.updatedAt - session.createdAt);
  session.durationMs = session.durationMs || session.spanMs;

  // MCP: Set → массив инструментов.
  const available = {};
  for (const [server, tools] of Object.entries(session.mcpAvailable)) {
    available[server] = [...tools].sort();
  }
  session.mcpAvailable = available;

  // Модели: Map → отсортированный массив.
  session.models = [...modelBuckets.values()].sort((a, b) => b.tokens.total - a.tokens.total);

  // Субагенты: дескрипторы + каталог дочерних сессий.
  const usedCatalogs = new Set();
  session.subagents = descriptors.map((descriptor) => {
    const catalog = catalogs.find((item) => item.label === descriptor.label && !usedCatalogs.has(item));
    if (catalog) usedCatalogs.add(catalog);
    return {
      label: descriptor.label,
      provider: descriptor.provider,
      model: descriptor.model,
      reasoningEffort: descriptor.reasoningEffort,
      mode: descriptor.mode || catalog?.mode || null,
      childId: catalog?.childId || null,
    };
  });
  for (const catalog of catalogs) {
    if (usedCatalogs.has(catalog)) continue;
    session.subagents.push({
      label: catalog.label || 'субагент',
      provider: null,
      model: null,
      reasoningEffort: null,
      mode: catalog.mode || null,
      childId: catalog.childId || null,
    });
  }

  // Юниты из счётчиков инструментов.
  const units = {};
  for (const [name, calls] of Object.entries(session.toolCounts)) {
    const unit = unitOfTool(name);
    if (!units[unit]) units[unit] = { calls: 0, tools: {} };
    units[unit].calls += calls;
    units[unit].tools[name] = calls;
  }
  session.units = units;

  if (!session.title) {
    const fallback = session.prompts[0] || (session.origin === 'subagent' ? 'Сессия субагента' : 'Сессия');
    session.title = truncate(fallback, 70);
    session.titleSource = session.titleSource || 'fallback';
  }

  const age = Date.now() - (session.mtimeMs || 0);
  const reason = session.lastTurnReason?.kind;
  if (session.openTurn && age < 5 * 60_000) {
    session.status = 'active';
  } else if (reason === 'error') {
    session.status = 'error';
  } else if (reason === 'interrupted' || reason === 'aborted') {
    session.status = 'interrupted';
  } else if (session.turns > 0) {
    session.status = session.openTurn ? 'active' : 'done';
  } else {
    session.status = 'empty';
  }

  session.durationMs = Math.max(0, session.durationMs);
  delete session.toolCounts;
  return session;
}

/* ─────────────────────────────── кеш ────────────────────────────────── */

function sessionDataFile(id) {
  const safe = String(id).replace(/[^A-Za-z0-9._-]/g, '_');
  return path.join(SESSION_DATA_DIR, `${safe}.json`);
}

function saveSession(session) {
  writeAtomic(sessionDataFile(session.id), JSON.stringify(session, null, 2));
}

function listSessionFiles() {
  const result = [];
  if (!fs.existsSync(SESSIONS_ROOT)) return result;

  for (const workspaceDir of fs.readdirSync(SESSIONS_ROOT)) {
    const workspacePath = path.join(SESSIONS_ROOT, workspaceDir);
    let workspaceStat;
    try {
      workspaceStat = fs.statSync(workspacePath);
    } catch {
      continue;
    }
    if (!workspaceStat.isDirectory()) continue;

    for (const sessionDir of fs.readdirSync(workspacePath)) {
      const sessionPath = path.join(workspacePath, sessionDir);
      let sessionStat;
      try {
        sessionStat = fs.statSync(sessionPath);
      } catch {
        continue;
      }
      if (!sessionStat.isDirectory()) continue;

      for (const name of ['session.v4.jsonl.zstd', 'session.v4.jsonl.zst']) {
        const file = path.join(sessionPath, name);
        if (fs.existsSync(file)) {
          result.push(file);
          break;
        }
      }
    }
  }

  return result;
}

function loadSessions({ reindex = false } = {}) {
  ensureDirs();
  const state = reindex
    ? { version: STATE_VERSION, analyzerVersion: ANALYZER_VERSION, files: {} }
    : readJson(STATE_PATH, { version: STATE_VERSION, analyzerVersion: ANALYZER_VERSION, files: {} });

  if (state.version !== STATE_VERSION || state.analyzerVersion !== ANALYZER_VERSION) {
    state.version = STATE_VERSION;
    state.analyzerVersion = ANALYZER_VERSION;
    state.files = {};
  }
  if (!state.files || typeof state.files !== 'object') state.files = {};

  const files = listSessionFiles();
  const fileSet = new Set(files.map((file) => path.resolve(file)));
  const sessions = [];
  const seen = new Set();

  for (const file of files) {
    const key = path.resolve(file);
    let stat;
    try {
      stat = fs.statSync(file);
    } catch {
      continue;
    }

    let session = null;
    const cached = state.files[key];
    if (!reindex && cached && cached.size === stat.size && cached.mtimeMs === stat.mtimeMs && cached.id) {
      session = readJson(sessionDataFile(cached.id), null);
    }

    if (!session) {
      try {
        session = analyzeSession(file, stat);
        saveSession(session);
      } catch (error) {
        console.error(`[session-analytics] не удалось разобрать ${file}: ${error.message}`);
        continue;
      }
    }

    state.files[key] = {
      size: stat.size,
      mtimeMs: stat.mtimeMs,
      id: session.id,
      analyzedAt: new Date().toISOString(),
    };

    if (seen.has(session.id)) {
      const previous = sessions.findIndex((item) => item.id === session.id);
      if (previous >= 0 && sessions[previous].mtimeMs >= session.mtimeMs) continue;
      if (previous >= 0) sessions.splice(previous, 1);
    }

    seen.add(session.id);
    sessions.push(session);
  }

  for (const key of Object.keys(state.files)) {
    if (!fileSet.has(key)) delete state.files[key];
  }

  state.updatedAt = new Date().toISOString();
  writeAtomic(STATE_PATH, JSON.stringify(state, null, 2));

  return sessions.sort((a, b) => (b.updatedAt || 0) - (a.updatedAt || 0));
}

/* ──────────────────────────── агрегация ─────────────────────────────── */

function aggregate(sessions, since) {
  const scoped = (since ? sessions.filter((session) => (session.createdAt || 0) >= since) : sessions)
    .slice()
    .sort((a, b) => (b.updatedAt || 0) - (a.updatedAt || 0));
  for (const session of scoped) delete session.duplicateOf;

  // Точные копии сессий (DSH сохраняет дубли с суффиксом «(2)»): в отчёте
  // показываем все, но токены и метрики считаем один раз.
  const byFingerprint = new Map();
  for (const session of scoped) {
    if (!session.fingerprint) continue;
    if (!byFingerprint.has(session.fingerprint)) byFingerprint.set(session.fingerprint, []);
    byFingerprint.get(session.fingerprint).push(session);
  }
  const duplicates = [];
  for (const group of byFingerprint.values()) {
    if (group.length < 2) continue;
    group.sort(
      (a, b) => (a.createdAt || 0) - (b.createdAt || 0) || String(a.id).localeCompare(String(b.id)),
    );
    const primary = group[0];
    for (let i = 1; i < group.length; i += 1) {
      group[i].duplicateOf = primary.id;
      duplicates.push(group[i]);
    }
  }

  const unique = scoped.filter((session) => !session.duplicateOf);

  // Копии-снимки: содержимое сессии целиком является префиксом более длинной
  // сессии (дублирование диалога в GUI). Токены копии уже входят в длинную
  // сессию, поэтому в суммах учитываем только её.
  const textCache = new Map();
  const normalizedText = (session) => {
    if (!textCache.has(session.file)) {
      try {
        textCache.set(session.file, readNormalizedSession(session.file));
      } catch {
        textCache.set(session.file, null);
      }
    }
    return textCache.get(session.file);
  };
  for (const shorter of [...unique].sort((a, b) => (a.normalizedLength || 0) - (b.normalizedLength || 0))) {
    if (!shorter.normalizedLength || !shorter.normalizedHeadHash) continue;
    const candidates = unique
      .filter(
        (candidate) =>
          candidate !== shorter &&
          (candidate.normalizedLength || 0) > shorter.normalizedLength &&
          candidate.normalizedHeadHash === shorter.normalizedHeadHash,
      )
      .sort((a, b) => a.normalizedLength - b.normalizedLength);
    if (candidates.length === 0) continue;
    const shortText = normalizedText(shorter);
    if (!shortText) continue;
    for (const longer of candidates) {
      const longText = normalizedText(longer);
      if (longText && longText.startsWith(shortText)) {
        shorter.snapshotOf = longer.id;
        duplicates.push(shorter);
        break;
      }
    }
  }

  const counted = unique.filter((session) => !session.snapshotOf);
  const ids = new Set(counted.map((session) => session.id));
  const children = new Map();
  for (const session of counted) {
    if (!session.parentId || !ids.has(session.parentId)) continue;
    if (!children.has(session.parentId)) children.set(session.parentId, []);
    children.get(session.parentId).push(session);
  }

  const treeTotals = new Map();
  const visiting = new Set();
  const treeTotal = (session) => {
    if (treeTotals.has(session.id)) return treeTotals.get(session.id);
    if (visiting.has(session.id)) return session.tokens.total;
    visiting.add(session.id);
    let total = session.tokens.total;
    for (const child of children.get(session.id) || []) total += treeTotal(child);
    visiting.delete(session.id);
    treeTotals.set(session.id, total);
    return total;
  };
  for (const session of counted) treeTotal(session);

  const totals = {
    sessions: scoped.length,
    unique: counted.length,
    duplicates: duplicates.length,
    roots: counted.filter((session) => !session.parentId).length,
    subagents: counted.filter((session) => session.parentId).length,
    turns: 0,
    steps: 0,
    messages: 0,
    errors: 0,
    toolErrors: 0,
    retries: 0,
    pluginOps: 0,
    toolTimeMs: 0,
    tokens: emptyTokens(),
    createdAt: null,
    updatedAt: null,
  };

  const models = new Map();
  const skills = new Map();
  const skillReads = new Map();
  const mcp = new Map();
  const units = new Map();
  const pluginOps = [];

  for (const session of counted) {
    totals.turns += session.turns;
    totals.steps += session.steps;
    totals.messages += session.messages;
    totals.errors += session.errors.length;
    totals.toolErrors += session.toolErrors;
    totals.retries += session.retries;
    totals.pluginOps += session.pluginOps.length;
    totals.toolTimeMs += session.toolTimeMs;
    addTokens(totals.tokens, session.tokens);
    if (!totals.createdAt || session.createdAt < totals.createdAt) totals.createdAt = session.createdAt;
    if (!totals.updatedAt || session.updatedAt > totals.updatedAt) totals.updatedAt = session.updatedAt;

    for (const bucket of session.models) {
      const key = `${bucket.provider}/${bucket.model}`;
      let entry = models.get(key);
      if (!entry) {
        entry = {
          provider: bucket.provider,
          model: bucket.model,
          messages: 0,
          sessions: new Set(),
          tokens: emptyTokens(),
        };
        models.set(key, entry);
      }
      entry.messages += bucket.messages;
      entry.sessions.add(session.id);
      addTokens(entry.tokens, bucket.tokens);
    }

    for (const [name, calls] of Object.entries(session.skills)) {
      let entry = skills.get(name);
      if (!entry) {
        entry = { name, calls: 0, sessions: new Set() };
        skills.set(name, entry);
      }
      entry.calls += calls;
      entry.sessions.add(session.id);
    }

    for (const [name, calls] of Object.entries(session.skillReads)) {
      let entry = skillReads.get(name);
      if (!entry) {
        entry = { name, calls: 0, sessions: new Set() };
        skillReads.set(name, entry);
      }
      entry.calls += calls;
      entry.sessions.add(session.id);
    }

    for (const [server, tools] of Object.entries(session.mcpAvailable)) {
      let entry = mcp.get(server);
      if (!entry) {
        entry = { server, tools: 0, calls: 0, availableIn: new Set(), usedIn: new Set() };
        mcp.set(server, entry);
      }
      entry.tools = Math.max(entry.tools, tools.length);
      entry.availableIn.add(session.id);
    }

    for (const [server, calls] of Object.entries(session.mcpUsed)) {
      let entry = mcp.get(server);
      if (!entry) {
        entry = { server, tools: 0, calls: 0, availableIn: new Set(), usedIn: new Set() };
        mcp.set(server, entry);
      }
      entry.calls += calls;
      entry.usedIn.add(session.id);
    }

    for (const [unit, details] of Object.entries(session.units)) {
      if (unit.startsWith('mcp:')) continue;
      let entry = units.get(unit);
      if (!entry) {
        entry = { unit, calls: 0, sessions: new Set(), tools: {} };
        units.set(unit, entry);
      }
      entry.calls += details.calls;
      entry.sessions.add(session.id);
      for (const [tool, calls] of Object.entries(details.tools)) {
        entry.tools[tool] = (entry.tools[tool] || 0) + calls;
      }
    }

    for (const op of session.pluginOps) {
      pluginOps.push({ ...op, sessionId: session.id, sessionTitle: session.title });
    }
  }

  const modelList = [...models.values()].sort((a, b) => b.tokens.total - a.tokens.total);
  const skillList = [...skills.values()].sort((a, b) => b.calls - a.calls);
  const skillReadList = [...skillReads.values()].sort((a, b) => b.calls - a.calls);
  const mcpList = [...mcp.values()].sort((a, b) => b.calls - a.calls || b.tools - a.tools);
  const unitList = [...units.values()].sort((a, b) => b.calls - a.calls);
  pluginOps.sort((a, b) => (b.time || 0) - (a.time || 0));

  return {
    scoped,
    unique: counted,
    duplicates,
    children,
    treeTotals,
    totals,
    models: modelList,
    skills: skillList,
    skillReads: skillReadList,
    mcp: mcpList,
    units: unitList,
    pluginOps,
    since,
  };
}

/* ───────────────────────────── рендеринг ────────────────────────────── */

function markdownTable(headers, rows) {
  const lines = [
    `| ${headers.join(' | ')} |`,
    `| ${headers.map(() => '---').join(' | ')} |`,
  ];
  for (const row of rows) lines.push(`| ${row.join(' | ')} |`);
  return lines.join('\n');
}

function statusCell(status) {
  const info = STATUS[status] || STATUS.empty;
  return `${info.icon} ${info.label}`;
}

function isCopy(session) {
  return Boolean(session.duplicateOf || session.snapshotOf);
}

function unitLabel(unit) {
  return UNIT_LABELS[unit] || unit;
}

function renderSessionDetail(session, agg) {
  const lines = [];
  const tree = agg?.treeTotals?.get(session.id) ?? session.tokens.total;
  const children = agg?.children?.get(session.id) ?? [];

  lines.push(`### ${statusCell(session.status)} · ${mdCell(truncate(session.title, 90))}`);
  lines.push('');
  if (isCopy(session)) {
    const original = session.duplicateOf || session.snapshotOf;
    lines.push(`_Копия сессии \`${shortId(original)}\`; в суммах учтена исходная сессия._`);
    lines.push('');
  }
  const spanNote =
    session.spanMs && session.spanMs > session.durationMs * 1.5 ? ` · охват ${fmtDuration(session.spanMs)}` : '';
  lines.push(
    `\`${session.id}\` · ${mdCell(session.project)} · ${fmtDateTime(session.createdAt)} · ${fmtDuration(session.durationMs)}${spanNote}`,
  );
  if (session.cwd && session.cwd !== '—') lines.push(`\`${session.cwd}\``);
  lines.push('');

  const modelLines = session.models.map(
    (bucket) =>
      `- **Модель:** \`${bucket.provider}/${bucket.model}\` — ${num(bucket.messages)} отв.; ${tokenSummary(bucket.tokens)}.`,
  );
  if (modelLines.length === 0) modelLines.push('- **Модель:** нет ответов модели.');
  lines.push(...modelLines);

  const ownTokens = session.tokens.total;
  const treeNote = tree !== ownTokens ? ` (с субагентами ${num(tree)})` : '';
  lines.push(`- **Токены:** ${tokenSummary(session.tokens)}; всего **${num(ownTokens)}**${treeNote}.`);
  lines.push(
    `- **Ходы:** ${num(session.turns)} · шаги: ${num(session.steps)} · сообщений: ${num(session.messages)} · инструментов: ${num(
      Object.values(session.units).reduce((sum, unit) => sum + unit.calls, 0),
    )}`,
  );

  const toolPairs = Object.entries(session.units)
    .flatMap(([unit, details]) => Object.entries(details.tools).map(([tool, calls]) => [tool, calls, unit]))
    .sort((a, b) => b[1] - a[1])
    .slice(0, 8);
  if (toolPairs.length > 0) {
    lines.push(`- **Топ инструментов:** ${toolPairs.map(([tool, calls]) => `${tool} ×${calls}`).join(', ')}.`);
  }

  const skills = Object.entries(session.skills);
  const reads = Object.entries(session.skillReads);
  if (skills.length > 0 || reads.length > 0) {
    const parts = [];
    for (const [name, calls] of skills) parts.push(`${name} ×${calls}`);
    for (const [name, calls] of reads) parts.push(`${name} (SKILL.md ×${calls})`);
    lines.push(`- **Скиллы:** ${parts.join(', ')}.`);
  } else {
    lines.push('- **Скиллы:** не загружались.');
  }

  const mcpAvailable = Object.entries(session.mcpAvailable)
    .map(([server, tools]) => `${server} (${tools.length})`)
    .join(', ');
  const mcpUsed = Object.entries(session.mcpUsed)
    .map(([server, calls]) => `${server} ×${calls}`)
    .join(', ');
  if (mcpAvailable || mcpUsed) {
    lines.push(`- **MCP:** доступны ${mcpAvailable || '—'}; вызовы: ${mcpUsed || 'нет'}.`);
  } else {
    lines.push('- **MCP:** серверы не подключены.');
  }

  if (session.pluginOps.length > 0) {
    const ops = session.pluginOps
      .map((op) => `${op.action}${op.target && op.target !== '—' ? ` (${op.target})` : ''}${op.ok === false ? ' ✕' : ''}`)
      .join('; ');
    lines.push(`- **Плагины:** ${ops}.`);
  } else {
    const extensionUnits = Object.keys(session.units).filter((unit) => EXTENSION_UNITS.has(unit));
    if (extensionUnits.length > 0) {
      lines.push(
        `- **Плагины/расширения:** ${extensionUnits
          .map((unit) => `${unitLabel(unit)} ×${session.units[unit].calls}`)
          .join(', ')}.`,
      );
    } else {
      lines.push('- **Плагины:** операций не было.');
    }
  }

  if (session.subagents.length > 0) {
    const subagents = session.subagents
      .map((agent) => `${agent.label}${agent.model ? ` (${agent.model})` : ''}${agent.childId ? ` → ${shortId(agent.childId)}` : ''}`)
      .join('; ');
    lines.push(`- **Субагенты:** ${subagents}.`);
  }

  if (session.errors.length > 0) {
    lines.push(`- **Ошибки:** ${session.errors.map((error) => `ход ${error.turn}: ${error.message}`).join('; ')}.`);
  } else if (session.toolErrors > 0) {
    lines.push(`- **Ошибки:** инструменты вернули ошибку ${num(session.toolErrors)} раз.`);
  } else {
    lines.push('- **Ошибки:** нет.');
  }

  if (children.length > 0) {
    lines.push('');
    lines.push(
      `Дочерние сессии: ${children
        .map((child) => `${statusCell(child.status)} ${mdCell(truncate(child.title, 40))} (\`${shortId(child.id)}\`, ${num(child.tokens.total)} токенов)`)
        .join('; ')}.`,
    );
  }

  lines.push('');
  return lines.join('\n');
}

function renderReport(agg, profile, generatedAt) {
  const { scoped, totals, models, skills, skillReads, mcp, units, pluginOps, treeTotals, children } = agg;
  const roots = scoped.filter((session) => !session.parentId);
  const subagents = scoped.filter((session) => session.parentId);
  const lines = [];

  lines.push('# Аналитика сессий DeepSeek Harness');
  lines.push('');
  lines.push(
    `_Сформировано ${fmtDateTime(generatedAt.getTime())} · DSH ${profile.version || '—'} · профиль \`desktop\` · сессий: ${num(
      totals.sessions,
    )}_`,
  );
  if (agg.since) lines.push(`_Фильтр: сессии с ${fmtDateTime(agg.since)}._`);
  lines.push('');

  /* ── сводка ── */
  const cacheShare = totals.tokens.total > 0 ? Math.round((totals.tokens.cacheRead / totals.tokens.total) * 100) : 0;
  const skillNames = skills.map((skill) => skill.name);
  const mcpUsedServers = mcp.filter((server) => server.calls > 0).length;
  const topModel = models[0];

  lines.push('## Сводка');
  lines.push('');
  lines.push(
    markdownTable(
      ['Метрика', 'Значение'],
      [
      [
        'Сессий',
        `${num(totals.sessions)} — ${num(totals.roots)} основных, ${num(totals.subagents)} субагентов` +
          (totals.duplicates > 0 ? `; копий: ${num(totals.duplicates)} (учтены один раз)` : ''),
      ],
        [
          'Период',
          totals.createdAt && totals.updatedAt
            ? `${fmtDateTime(totals.createdAt)} — ${fmtDateTime(totals.updatedAt)}`
            : '—',
        ],
        ['Токенов всего (с кэшем)', `**${num(totals.tokens.total)}**`],
        ['· вход без кэша', num(totals.tokens.input)],
        ['· кэш-чтение', `${num(totals.tokens.cacheRead)} (${cacheShare}%)`],
        ['· выход', num(totals.tokens.output)],
        ['· без кэш-чтения', num(totals.tokens.input + totals.tokens.output + totals.tokens.cacheWrite)],
        ...(totals.tokens.reasoning ? [['· reasoning', num(totals.tokens.reasoning)]] : []),
        ['Ходы / шаги', `${num(totals.turns)} / ${num(totals.steps)}`],
        ['Ответов модели', num(totals.messages)],
        ['Ошибок сессий / инструментов', `${num(totals.errors)} / ${num(totals.toolErrors)}`],
        ['Повторов LLM-запросов', num(totals.retries)],
        [
          'Скиллы',
          skills.length > 0 ? `${num(skills.length)}: ${skillNames.slice(0, 5).join(', ')}${skillNames.length > 5 ? '…' : ''}` : 'не загружались',
        ],
        ['MCP', mcp.length > 0 ? `${num(mcpUsedServers)} из ${num(mcp.length)} серверов использовались` : 'серверы не подключены'],
        ['Операций с плагинами', num(totals.pluginOps)],
        ...(topModel ? [['Топ модель', `\`${topModel.provider}/${topModel.model}\` — ${num(topModel.tokens.total)} токенов`]] : []),
      ],
    ),
  );
  lines.push('');

  /* ── модели ── */
  lines.push('## Модели');
  lines.push('');
  if (models.length === 0) {
    lines.push('_Нет ответов модели._');
  } else {
    lines.push(
      markdownTable(
        ['Модель', 'Провайдер', 'Сессий', 'Ответов', 'Вход', 'Кэш', 'Выход', 'Всего'],
        models.map((model) => [
          `\`${mdCell(model.model)}\``,
          mdCell(model.provider),
          num(model.sessions.size),
          num(model.messages),
          num(model.tokens.input),
          num(model.tokens.cacheRead),
          num(model.tokens.output),
          `**${num(model.tokens.total)}**`,
        ]),
      ),
    );
  }
  lines.push('');

  /* ── скиллы ── */
  lines.push('## Скиллы');
  lines.push('');
  if (skills.length === 0 && skillReads.length === 0) {
    lines.push('_В этих сессиях скиллы не загружались._');
  } else {
    if (skills.length > 0) {
      lines.push(
        markdownTable(
          ['Скилл', 'Загрузок (`skill`)', 'Сессии'],
          skills.map((skill) => [`\`${mdCell(skill.name)}\``, num(skill.calls), num(skill.sessions.size)]),
        ),
      );
    }
    if (skillReads.length > 0) {
      lines.push('');
      lines.push('Косвенные чтения `SKILL.md`:');
      lines.push('');
      lines.push(
        markdownTable(
          ['Скилл', 'Чтений', 'Сессии'],
          skillReads.map((skill) => [`\`${mdCell(skill.name)}\``, num(skill.calls), num(skill.sessions.size)]),
        ),
      );
    }
  }
  lines.push('');

  /* ── MCP ── */
  lines.push('## MCP');
  lines.push('');
  if (mcp.length === 0) {
    lines.push('_MCP-серверы не были подключены ни в одной сессии._');
  } else {
    lines.push(
      markdownTable(
        ['Сервер', 'Инструментов', 'Вызовов', 'Сессии'],
        mcp.map((server) => [
          `\`${mdCell(server.server)}\``,
          num(server.tools),
          num(server.calls),
          server.usedIn.size > 0 ? num(server.usedIn.size) : '—',
        ]),
      ),
    );
    if (mcpUsedServers === 0) {
      lines.push('');
      lines.push('_Серверы были доступны агенту, но ни один инструмент MCP не вызывался._');
    }
  }
  lines.push('');

  /* ── плагины ── */
  lines.push('## Плагины и расширения');
  lines.push('');
  const extensionUnits = units.filter((unit) => EXTENSION_UNITS.has(unit.unit));
  if (extensionUnits.length === 0 && pluginOps.length === 0) {
    lines.push('_Расширения DSH (расписание, субагенты, workflow, present, web, skill) в сессиях не вызывались._');
  } else {
    if (extensionUnits.length > 0) {
      lines.push('### Юниты, которые вызывались');
      lines.push('');
      lines.push(
        markdownTable(
          ['Юнит', 'Вызовов', 'Сессии', 'Инструменты'],
          extensionUnits.map((unit) => [
            unitLabel(unit.unit),
            num(unit.calls),
            num(unit.sessions.size),
            Object.entries(unit.tools)
              .sort((a, b) => b[1] - a[1])
              .map(([tool, calls]) => `${tool} ×${calls}`)
              .slice(0, 4)
              .join(', '),
          ]),
        ),
      );
      lines.push('');
    }

    if (pluginOps.length > 0) {
      lines.push('### Операции `plugin_manager`');
      lines.push('');
      lines.push(
        markdownTable(
          ['Когда', 'Сессия', 'Действие', 'Объект', 'Итог'],
          pluginOps.slice(0, 20).map((op) => [
            fmtDayTime(op.time),
            `\`${shortId(op.sessionId)}\``,
            op.action,
            mdCell(op.target),
            op.ok === true ? 'ok' : op.ok === false ? '✕' : '—',
          ]),
        ),
      );
      lines.push('');
    }
  }

  lines.push('### Профиль');
  lines.push('');
  lines.push(`- Бандлы: ${profile.bundles.map((bundle) => `\`${bundle}\``).join(', ') || '—'}.`);
  if (profile.packages.length > 0) {
    lines.push(`- Пакеты в node_modules профиля: ${profile.packages.map((name) => `\`${name}\``).join(', ')}.`);
  }
  lines.push(`- MCP-серверы конфига: ${profile.mcpServers.map((name) => `\`${name}\``).join(', ') || '—'}.`);
  if (profile.patchEntries.length > 0) {
    lines.push(`- Вставки конфига: \`${profile.patchEntries.join('`, `')}\`.`);
  }
  lines.push('');

  /* ── сессии ── */
  lines.push('## Сессии');
  lines.push('');
  if (roots.length === 0 && subagents.length === 0) {
    lines.push('_Сессий не найдено._');
  } else {
    if (roots.length > 0) {
      lines.push('### Основные');
      lines.push('');
      lines.push(
        markdownTable(
          ['Обновлена', 'Сессия', 'Модель', 'Токены', 'Ходы', 'Скиллы', 'MCP', 'Плагины'],
          roots.map((session) => {
            const modelNames = session.models.map((bucket) => bucket.model);
            const modelCellText =
              modelNames.length === 0
                ? '—'
                : `${mdCell(modelNames.slice(0, 2).join(', '))}${modelNames.length > 2 ? ` +${modelNames.length - 2}` : ''}`;
            const tree = treeTotals.get(session.id) ?? session.tokens.total;
            const tokensCell =
              tree !== session.tokens.total ? `${num(session.tokens.total)} (${num(tree)})` : num(session.tokens.total);
            const skillNamesCell =
              Object.keys(session.skills).length > 0
                ? mdCell(Object.keys(session.skills).slice(0, 2).join(', ')) +
                  (Object.keys(session.skills).length > 2 ? ` +${Object.keys(session.skills).length - 2}` : '')
                : '—';
            const mcpUsedCell =
              Object.keys(session.mcpUsed).length > 0 ? mdCell(Object.keys(session.mcpUsed).join(', ')) : '—';
            const pluginUnits = Object.keys(session.units).filter((unit) => EXTENSION_UNITS.has(unit));
            const pluginCell =
              pluginUnits.length > 0
                ? mdCell(pluginUnits.slice(0, 2).map((unit) => `${unitLabel(unit)}×${session.units[unit].calls}`).join(', ')) +
                  (pluginUnits.length > 2 ? ` +${pluginUnits.length - 2}` : '')
                : session.pluginOps.length > 0
                  ? `plugin_manager×${session.pluginOps.length}`
                  : '—';

            return [
              fmtDayTime(session.updatedAt),
              `${isCopy(session) ? '⧉ ' : ''}${STATUS[session.status].icon} **${mdCell(truncate(session.title, 48))}** · \`${shortId(session.id)}\``,
              modelCellText,
              tokensCell,
              num(session.turns),
              skillNamesCell,
              mcpUsedCell,
              pluginCell,
            ];
          }),
        ),
      );
      lines.push('');
      lines.push('_В скобках у токенов — сумма с дочерними сессиями субагентов. ⧉ — копия другой сессии (в суммах учтена один раз)._');
      lines.push('');
    }

    if (subagents.length > 0) {
      lines.push('### Субагенты');
      lines.push('');
      lines.push(
        markdownTable(
          ['Обновлён', 'Субагент', 'Родитель', 'Модель', 'Токены', 'Ходы'],
          subagents.map((session) => [
            fmtDayTime(session.updatedAt),
            `${isCopy(session) ? '⧉ ' : ''}${STATUS[session.status].icon} ${mdCell(truncate(session.title, 44))} · \`${shortId(session.id)}\``,
            session.parentId ? `\`${shortId(session.parentId)}\`` : '—',
            session.models.length > 0 ? mdCell(session.models.map((bucket) => bucket.model).join(', ')) : '—',
            num(session.tokens.total),
            num(session.turns),
          ]),
        ),
      );
      lines.push('');
    }
  }

  /* ── детали ── */
  const recent = roots
    .filter((session) => !isCopy(session) && (session.messages > 0 || session.turns > 0))
    .slice(0, 3);
  if (recent.length > 0) {
    lines.push('## Последние сессии подробно');
    lines.push('');
    for (const session of recent) {
      lines.push(renderSessionDetail(session, agg));
    }
  }

  /* ── подвал ── */
  lines.push('---');
  lines.push('');
  lines.push(
    '_Обновляется задачей планировщика «DSH Session Analytics» каждые 10 минут. Ручной запуск: `run.cmd` (полный пересбор — `run.cmd --reindex`)._',
  );
  lines.push(
    '_Отчёт содержит заголовки и метаданные сессий; хранится локально в `~/.dsh/session-analytics` и не коммитится в git._',
  );
  lines.push('');

  return lines.join('\n');
}

/* ───────────────────────────── JSON-сводка ──────────────────────────── */

function jsonSummary(agg, profile, generatedAt) {
  return {
    generatedAt: generatedAt.toISOString(),
    dshVersion: profile.version,
    profile: {
      bundles: profile.bundles,
      mcpServers: profile.mcpServers,
      patchEntries: profile.patchEntries,
      packages: profile.packages,
    },
    totals: {
      sessions: agg.totals.sessions,
      unique: agg.totals.unique,
      duplicates: agg.totals.duplicates,
      roots: agg.totals.roots,
      subagents: agg.totals.subagents,
      turns: agg.totals.turns,
      steps: agg.totals.steps,
      messages: agg.totals.messages,
      errors: agg.totals.errors,
      toolErrors: agg.totals.toolErrors,
      retries: agg.totals.retries,
      pluginOps: agg.totals.pluginOps,
      tokens: agg.totals.tokens,
      createdAt: agg.totals.createdAt,
      updatedAt: agg.totals.updatedAt,
    },
    models: agg.models.map((model) => ({
      provider: model.provider,
      model: model.model,
      messages: model.messages,
      sessions: model.sessions.size,
      tokens: model.tokens,
    })),
    skills: agg.skills.map((skill) => ({ name: skill.name, calls: skill.calls, sessions: skill.sessions.size })),
    skillReads: agg.skillReads.map((skill) => ({ name: skill.name, calls: skill.calls, sessions: skill.sessions.size })),
    mcp: agg.mcp.map((server) => ({
      server: server.server,
      tools: server.tools,
      calls: server.calls,
      availableIn: server.availableIn.size,
      usedIn: server.usedIn.size,
    })),
    units: agg.units.map((unit) => ({
      unit: unit.unit,
      calls: unit.calls,
      sessions: unit.sessions.size,
      tools: unit.tools,
    })),
    pluginOps: agg.pluginOps.length,
    sessions: agg.scoped.map((session) => ({
      id: session.id,
      parentId: session.parentId,
      duplicateOf: session.duplicateOf || null,
      snapshotOf: session.snapshotOf || null,
      title: session.title,
      status: session.status,
      project: session.project,
      cwd: session.cwd,
      createdAt: session.createdAt,
      updatedAt: session.updatedAt,
      durationMs: session.durationMs,
      turns: session.turns,
      steps: session.steps,
      models: session.models.map((bucket) => `${bucket.provider}/${bucket.model}`),
      tokens: session.tokens,
      treeTokens: agg.treeTotals.get(session.id) ?? session.tokens.total,
      skills: Object.keys(session.skills),
      mcpAvailable: session.mcpAvailable,
      mcpUsed: session.mcpUsed,
      units: Object.fromEntries(Object.entries(session.units).map(([unit, details]) => [unit, details.calls])),
      pluginOps: session.pluginOps.map((op) => ({ action: op.action, target: op.target, ok: op.ok })),
      errors: session.errors,
    })),
  };
}

/* ─────────────────────────────── запуск ─────────────────────────────── */

function acquireLock() {
  ensureDirs();
  try {
    const stat = fs.statSync(LOCK_PATH);
    if (Date.now() - stat.mtimeMs < LOCK_TTL_MS) return false;
  } catch {
    /* лок можно взять */
  }
  fs.writeFileSync(LOCK_PATH, String(process.pid), 'utf8');
  return true;
}

function releaseLock() {
  try {
    fs.unlinkSync(LOCK_PATH);
  } catch {
    /* уже удалён */
  }
}

function printSummary(agg, generatedAt) {
  const totals = agg.totals;
  const usedMcp = agg.mcp.filter((server) => server.calls > 0).length;
  console.log(`Аналитика сессий DSH · ${fmtDateTime(generatedAt.getTime())}`);
  console.log(
    `Сессий: ${num(totals.sessions)} (${num(totals.roots)} + ${num(totals.subagents)} субагентов${
      totals.duplicates > 0 ? `, копий ${num(totals.duplicates)}` : ''
    }) · токенов: ${num(totals.tokens.total)} · моделей: ${num(agg.models.length)}`,
  );
  console.log(
    `Скиллы: ${num(agg.skills.length)} · MCP: ${num(usedMcp)}/${num(agg.mcp.length)} серверов · плагины: ${num(
      totals.pluginOps,
    )} операций`,
  );
  console.log(`Отчёт: ${LATEST_PATH}`);
}

function runOnce({ quiet = false, json = false, since = null, reindex = false } = {}) {
  ensureDirs();
  if (!acquireLock()) {
    if (!quiet && !json) console.log('[session-analytics] уже выполняется, пропуск');
    return null;
  }

  try {
    const generatedAt = new Date();
    const profile = readProfile();
    const sessions = loadSessions({ reindex });
    const agg = aggregate(sessions, since);
    const report = renderReport(agg, profile, generatedAt);

    writeAtomic(LATEST_PATH, report);
    writeAtomic(path.join(REPORTS_DIR, `${todayISO()}.md`), report);
    writeAtomic(SUMMARY_PATH, JSON.stringify(jsonSummary(agg, profile, generatedAt), null, 2));

    if (json) {
      process.stdout.write(`${JSON.stringify(jsonSummary(agg, profile, generatedAt), null, 2)}\n`);
    } else if (!quiet) {
      printSummary(agg, generatedAt);
    }

    return agg;
  } finally {
    releaseLock();
  }
}

function printSessionDetail(identifier) {
  const sessions = loadSessions({});
  const normalize = (value) => String(value).toLowerCase().replace(/^session-/, '');
  const query = normalize(identifier);
  const session =
    sessions.find((item) => item.id === identifier) ||
    sessions.find((item) => normalize(item.id).startsWith(query)) ||
    sessions.find((item) => item.title.toLowerCase().includes(query));

  if (!session) {
    console.error(`[session-analytics] сессия не найдена: ${identifier}`);
    process.exitCode = 1;
    return;
  }

  const agg = aggregate(sessions, null);
  console.log(renderSessionDetail(session, agg));
}

function watchSessions() {
  runOnce({ quiet: true });
  let timer = null;
  const schedule = () => {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => {
      try {
        runOnce({ quiet: true });
      } catch (error) {
        console.error(`[session-analytics] ${error.message}`);
      }
    }, 4_000);
  };

  try {
    fs.watch(SESSIONS_ROOT, { recursive: true }, schedule);
    console.log(`[session-analytics] наблюдаю за ${SESSIONS_ROOT}`);
  } catch (error) {
    console.error(`[session-analytics] fs.watch недоступен: ${error.message}`);
  }

  setInterval(() => {
    try {
      runOnce({ quiet: true });
    } catch (error) {
      console.error(`[session-analytics] ${error.message}`);
    }
  }, 5 * 60_000);

  process.on('SIGINT', () => process.exit(0));
  process.on('SIGTERM', () => process.exit(0));
}

function printHelp() {
  console.log(`DSH Session Analytics — метрики сессий DeepSeek Harness.

Запуск:
  node session-analytics.mjs                 инкрементальный разбор + latest.md
  node session-analytics.mjs --reindex       пересобрать все сессии
  node session-analytics.mjs --session=ID    подробности одной сессии
  node session-analytics.mjs --since=7d      только сессии за последние 7 дней
  node session-analytics.mjs --watch         следить за изменениями
  node session-analytics.mjs --json          машиночитаемая сводка
  node session-analytics.mjs --quiet         без вывода в консоль

Данные: ~/.dsh/session-analytics (latest.md, reports/, data/).`);
}

function main() {
  const argv = process.argv.slice(2);
  const options = {
    quiet: argv.includes('--quiet') || argv.includes('-q'),
    json: argv.includes('--json'),
    watch: argv.includes('--watch'),
    reindex: argv.includes('--reindex'),
    help: argv.includes('--help') || argv.includes('-h'),
    session: null,
    since: null,
  };

  for (const arg of argv) {
    if (arg.startsWith('--session=')) options.session = arg.slice('--session='.length);
    if (arg.startsWith('--since=')) options.since = arg.slice('--since='.length);
  }

  if (options.help) {
    printHelp();
    return;
  }

  if (options.session) {
    printSessionDetail(options.session);
    return;
  }

  if (options.watch) {
    watchSessions();
    return;
  }

  runOnce({
    quiet: options.quiet,
    json: options.json,
    since: parseSince(options.since),
    reindex: options.reindex,
  });
}

try {
  main();
} catch (error) {
  console.error(`[session-analytics] ${error.stack || error.message}`);
  releaseLock();
  process.exitCode = 1;
}
