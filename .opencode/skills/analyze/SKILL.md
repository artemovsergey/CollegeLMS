---
name: analyze
description: Run comprehensive project analysis — spec, user stories, plugins, MCP, skills, readiness, phases, agents, models
---

# analyze — Project Analyzer

Run full project health check across 9 dimensions and output a Markdown report with ✅/❌/⚠️ status indicators.

## How to use

1. User types `/analyze` or asks for project analysis
2. Create todos for all 9 sections using `todowrite`
3. For each section, execute the listed checks (read files, run commands)
4. Collect evidence and output Markdown with ✅/❌/⚠️
5. Print the report directly to console

---

## 1. 📋 Technical Specification

Check that `docs/spec/task.md` exists, covers all services, and matches actual code.

- [ ] Read `docs/spec/task.md` — record line count and sections
- [ ] Verify 5 core services documented: SiteService, AuthService, ScheduleService, LearningService, TestingService
- [ ] Verify stack requirements: Postgres, .NET, Next.js, Docker, Nginx
- [ ] Cross-reference against actual `CollegeLMS.API/Controllers/` to detect undocumented endpoints
- [ ] Cross-reference against actual `CollegeLMS.API/Services/` to detect undocumented services

**Pass:** task.md exists, covers all 5 core services, no undocumented controllers/services
**Fail:** task.md missing, core service missing, undocumented endpoint found

---

## 2. 📖 User Stories

Check that `docs/spec/userstories.md` exists, parse UC status, compare with actual implementation.

- [ ] Read `docs/spec/userstories.md` — count total UC
- [ ] Identify status per priority: P0 (MVP), P1, P2, P3
- [ ] Count implemented (✅) vs partial (⚠️) vs not started (❌) vs deferred (⏸)
- [ ] Compare UC against actual controllers: `Get-ChildItem CollegeLMS.API/Controllers/ -Name`
- [ ] Check P0 coverage: how many P0 stories have matching controller/service

**Pass:** userstories.md exists, P0 100% implemented
**Warning:** P0 partially implemented, P1/P2 gap
**Fail:** file missing, no UC documented

---

## 3. 🔌 Plugins

Check that superpowers plugin is installed and accessible.

- [ ] Read `opencode.json` — extract `plugin` array
- [ ] Verify superpowers plugin source URL is valid
- [ ] Verify superpowers process skills exist: `Test-Path "$env:USERPROFILE\.cache\opencode\packages\superpowers\*"`
- [ ] List available superpowers skills: brainstorming, systematic-debugging, test-driven-development, writing-plans, executing-plans, etc.

**Pass:** superpowers plugin installed, process skills accessible
**Fail:** plugin missing, skills not found

---

## 4. 🔗 MCP Servers

Check that MCP servers are configured and healthy.

- [ ] Read `opencode.json` — `mcp` section
- [ ] Verify Playwright: `enabled: true`, command valid, env vars set
- [ ] Verify GitHub: `enabled: true`, command valid, `GITHUB_TOKEN` environment variable set
- [ ] Check for duplicate or conflicting MCP server definitions

**Pass:** Both Playwright and GitHub enabled, GITHUB_TOKEN set
**Warning:** One server disabled, GITHUB_TOKEN missing
**Fail:** Both disabled, config errors

---

## 5. 🛠️ Skills

Check that all skills have valid SKILL.md files and match references.

- [ ] List `.opencode/skills/` — count directories, verify each has SKILL.md
- [ ] List `.agents/skills/` — count directories, verify each has SKILL.md
- [ ] Cross-check skills referenced in `AGENTS.md` against actual skill directories
- [ ] Check for broken references: skills listed in AGENTS.md but missing on disk
- [ ] Check for orphan skills: skill directories on disk but not mentioned anywhere

**Pass:** All skills have SKILL.md, references match
**Warning:** Some skills missing references, orphan skills found
**Fail:** SKILL.md missing, broken references

---

## 6. 🚦 Development Readiness

Check that the project builds, tests pass, and working tree is clean.

- [ ] Run `dotnet build CollegeLMS.API\CollegeLMS.csproj --no-restore` — check exit code 0
- [ ] Run `dotnet test CollegeLMS.Tests\CollegeLMS.Tests.csproj --no-restore` — check exit code 0
- [ ] Run `cd frontend && npm run lint` — check exit code 0
- [ ] Run `git status --porcelain` — check for uncommitted changes
- [ ] Check git log for recent activity: `git log --oneline -5`

**Pass:** build OK, tests OK, lint OK, clean working tree
**Warning:** build or tests pass with warnings, dirty tree
**Fail:** build fails, tests fail, lint fails

---

## 7. 📊 Development Phases

Check phase progress per feature following the Full Feature Cycle from AGENTS.md.

- [ ] Check git log for phase commits: `git log --oneline --grep="phase" -30`
- [ ] List migrations: `Get-ChildItem CollegeLMS.API\Migrations\`
- [ ] Map which features have passed which gates (G1 build, G2 test, G3 dev, G4 e2e, G5 docker)
- [ ] Check each feature branch: `git branch -a`
- [ ] Verify migration files match entity count

**Pass:** All features have complete phase chain, migrations match entities
**Warning:** Some features partially through phases
**Fail:** No phase tracking, migrations missing

---

## 8. 🤖 Agents

Check that all agents are configured with proper model and description.

- [ ] Read `opencode.json` — `agent.subagent` section
- [ ] List all agents: BackendAgent, FrontendAgent, TesterAgent, AnalystAgent, DevOpsAgent
- [ ] Verify each has `model` and `description` fields
- [ ] Verify descriptions match actual role responsibilities from AGENTS.md

**Pass:** All 5 agents configured with model and description
**Warning:** Missing description, mismatch with AGENTS.md
**Fail:** Agent missing, missing model field

---

## 9. 🧠 Models

Check model consistency across all agent configurations.

- [ ] Extract model names from all agent configs
- [ ] Check consistency: all agents use same model
- [ ] Verify model name matches available models

**Pass:** Single consistent model across all agents
**Warning:** Mixed models (some different)
**Fail:** Model field missing, invalid model name

---

## 📈 Summary

After all sections complete, output a summary table:

```markdown
## 📈 Summary

| # | Section | Status |
|---|---------|--------|
| 1 | 📋 Technical Specification | ✅ |
| 2 | 📖 User Stories | ✅/⚠️/❌ |
| 3 | 🔌 Plugins | ✅ |
| 4 | 🔗 MCP Servers | ✅ |
| 5 | 🛠️ Skills | ✅ |
| 6 | 🚦 Development Readiness | ✅ |
| 7 | 📊 Development Phases | ⚠️ |
| 8 | 🤖 Agents | ✅ |
| 9 | 🧠 Models | ✅ |

**Overall: 🟢 Good / 🟡 Needs Attention / 🔴 Needs Work**
```

---
