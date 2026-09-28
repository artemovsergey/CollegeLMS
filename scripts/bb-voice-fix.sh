#!/usr/bin/env bash
# ---------------------------------------------------------------------------
# bb-voice-fix — восстановление голосового ввода в bb после обновления плагина
#
# Зачем: плагин voice-input@0.2.1 несовместим с bb 0.44.0 из коробки.
#   1) не объявляет функцию transcribe в AI-сервисе  -> плагин падает в error
#   2) не собирает webm-чанки, которые браузер отдаёт вразнобой
#         -> Groq отвечает 400 invalid_media_file
# Оба дефекта лечатся правкой бандла плагина. Правки слетают при
# `bb plugin update` — этот скрипт их применяет заново. Идемпотентен.
#
# Использование:  bash ~/bb-voice-fix.sh
# ---------------------------------------------------------------------------
set -uo pipefail

PLUGIN_ID="voice-input"
REPO_DIR="bb-plugin-voice-input"
MARKET="bb-community"
CACHE_GLOB="$HOME/.bb/plugins/cache/git/github.com/VKirill/$REPO_DIR"
ARTIFACTS="$HOME/.bb/plugin-host-artifacts/$PLUGIN_ID"

say()  { printf '\n\033[1m== %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32mOK\033[0m   %s\n' "$*"; }
warn() { printf '  \033[33m!\033[0m    %s\n' "$*"; }
die()  { printf '  \033[31mX\033[0m    %s\n' "$*" >&2; exit 1; }

TS=$(date +%Y%m%dT%H%M%S)

# --- тексты патчей ---------------------------------------------------------

OLD_SRV='kinds:["voice"]});let t="settings";'
NEW_SRV='kinds:["voice"],transcribe:async(t,d)=>{try{let m=await me(e,o,(await n()).machine||void 0),r=await o.call("transcribeDirect",{audioBase64:Buffer.from(await t.arrayBuffer()).toString("base64"),mimeType:(t&&t.type)||"audio/webm",filename:(t&&t.name)||undefined,prompt:(d&&d.hint)||null,timeoutMs:Wo},{hostId:m,timeoutMs:Wo});if(!r||typeof r.text!=="string")throw new Error("transcribeDirect returned no text");return r.text}catch(x){throw x}}});let t="settings";'

FIX_FN='function bbFixWebmChunkOrder(e){try{var t=Buffer.isBuffer(e)?e:new Uint8Array(e);if(t.length<64)return e;if(t[0]===26&&t[1]===69&&t[2]===223&&t[3]===163)return e;var n=-1,r;for(r=1;r<t.length-3;r++){if(t[r]===26&&t[r+1]===69&&t[r+2]===223&&t[r+3]===163){n=r;break}}if(n<0)return e;var a=-1;for(r=n+4;r<t.length-3;r++){if(t[r]===31&&t[r+1]===67&&t[r+2]===182&&t[r+3]===117){a=r;break}}if(a<0)return t.subarray(n);return Buffer.concat([t.subarray(n,a),t.subarray(a),t.subarray(0,n)]);}catch(i){return e}}'

OLD_HST='new Ak([t.audio],{type:t.mimeType})'
NEW_HST='new Ak([bbFixWebmChunkOrder(t.audio)],{type:t.mimeType})'
HST_ANCHOR='async function Oy(e,t){'

# --- 1. найти плагин -------------------------------------------------------

say "1. Каталог плагина"

resolve_dir() {
  local c hit
  for c in $(ls -dt "$CACHE_GLOB"/*/ 2>/dev/null); do
    [ -f "${c}dist/server.js" ] && { printf '%s' "${c%/}"; return 0; }
  done
  hit=$(find "$HOME/.bb/plugins/cache" -maxdepth 9 -type f -path '*/dist/server.js' 2>/dev/null \
        | grep -i "$PLUGIN_ID" | sort | tail -1)
  [ -n "$hit" ] && printf '%s' "$(dirname "$(dirname "$hit")")"
}

DIR=$(resolve_dir)
if [ -z "${DIR:-}" ]; then
  if bb plugin list 2>/dev/null | grep -qi "$PLUGIN_ID"; then
    die "плагин установлен, но файлы не найдены — выполните: bb plugin remove $PLUGIN_ID, затем запустите скрипт"
  fi
  warn "плагин не установлен — ставлю $PLUGIN_ID@$MARKET"
  bb plugin install "$PLUGIN_ID@$MARKET" --yes >/dev/null 2>&1 \
    || die "не удалось установить плагин"
  DIR=$(resolve_dir)
  [ -n "${DIR:-}" ] || die "плагин установлен, но каталог не найден"
  ok "установлен"
fi
[ -f "$DIR/dist/host.js" ] || die "нет $DIR/dist/host.js"
ok "$DIR"

# --- 2. патч server.js ----------------------------------------------------

say "2. server.js — объявление transcribe в AI-сервисе"
SRV="$DIR/dist/server.js"
if grep -qF 'kinds:["voice"],transcribe:' "$SRV"; then
  ok "уже пропатчен"
else
  N=$(grep -oF "$OLD_SRV" "$SRV" | wc -l)
  [ "$N" -eq 1 ] || die "якорь найден $N раз (нужен 1) — изменилась версия плагина, требуется ручная правка"
  [ -f "$SRV.orig" ] || cp "$SRV" "$SRV.orig"
  cp "$SRV" "$SRV.bak-$TS"
  OLD_SRV="$OLD_SRV" NEW_SRV="$NEW_SRV" python3 - "$SRV" <<'PY'
import os, sys
p = sys.argv[1]
s = open(p, encoding="utf-8").read()
old, new = os.environ["OLD_SRV"], os.environ["NEW_SRV"]
assert s.count(old) == 1, s.count(old)
open(p, "w", encoding="utf-8").write(s.replace(old, new, 1))
PY
  ok "пропатчен, бэкап $(basename "$SRV.bak-$TS")"
fi

# --- 3. патч host.js ------------------------------------------------------

say "3. host.js — сборка webm-чанков в правильном порядке"
HST="$DIR/dist/host.js"
if grep -qF 'bbFixWebmChunkOrder' "$HST"; then
  ok "уже пропатчен"
else
  N1=$(grep -oF "$OLD_HST" "$HST" | wc -l)
  N2=$(grep -oF "$HST_ANCHOR" "$HST" | wc -l)
  [ "$N1" -eq 1 ] || die "якорь отправки найден $N1 раз (нужен 1) — изменилась версия плагина"
  [ "$N2" -eq 1 ] || die "якорь функции найден $N2 раз (нужен 1) — изменилась версия плагина"
  [ -f "$HST.orig" ] || cp "$HST" "$HST.orig"
  cp "$HST" "$HST.bak-$TS"
  OLD_HST="$OLD_HST" NEW_HST="$NEW_HST" FIX_FN="$FIX_FN" HST_ANCHOR="$HST_ANCHOR" python3 - "$HST" <<'PY'
import os, sys
p = sys.argv[1]
s = open(p, encoding="utf-8").read()
old, new = os.environ["OLD_HST"], os.environ["NEW_HST"]
fn, anchor = os.environ["FIX_FN"], os.environ["HST_ANCHOR"]
assert s.count(old) == 1 and s.count(anchor) == 1
s = s.replace(old, new, 1).replace(anchor, fn + anchor, 1)
open(p, "w", encoding="utf-8").write(s)
PY
  ok "пропатчен, бэкап $(basename "$HST.bak-$TS")"
fi

# --- 4. синтаксис ---------------------------------------------------------

say "4. Проверка синтаксиса"
node --check "$SRV" && ok "server.js" || die "server.js сломан"
node --check "$HST" && ok "host.js"   || die "host.js сломан"

# --- 5. артефакт хоста ----------------------------------------------------

say "5. Синхронизация host-артефакта bb"
SHA=$(sha256sum "$HST" | cut -d' ' -f1)
DEST="$ARTIFACTS/$SHA"
mkdir -p "$DEST" || die "не создать $DEST"
chmod 700 "$DEST"
cp -f "$HST" "$DEST/host.mjs" || die "не скопировать host.mjs"
chmod 600 "$DEST/host.mjs"
ok "sha256 ${SHA:0:16}… -> $DEST/host.mjs"

META="$DIR/dist/host.meta.json"
if [ -f "$META" ]; then
  SHA="$SHA" python3 - "$META" <<'PY'
import os, re, sys
p, sha = sys.argv[1], os.environ["SHA"]
s = open(p, encoding="utf-8").read()
if f'"{sha}"' in s:
    print("   meta.json уже актуален")
    sys.exit(0)
s2 = re.sub(r'("artifactDigest"\s*:\s*")[0-9a-f]{64}(\s*")', r"\g<1>" + sha + r"\g<2>", s, count=1)
if s2 == s:
    sys.exit("поле artifactDigest не найдено в host.meta.json")
open(p, "w", encoding="utf-8").write(s2)
print("   meta.json: artifactDigest обновлён")
PY
  [ $? -eq 0 ] || die "не обновить host.meta.json"
else
  warn "нет host.meta.json — пропускаю"
fi

# --- 6. перезапуск --------------------------------------------------------

say "6. Перезапуск bb"
sudo systemctl restart bb-app || die "не перезапустить bb-app"
sleep 8
sudo systemctl is-active bb-app | grep -q active && ok "bb-app: active" || die "bb-app не активен"

# --- 7. конфигурация и привязка задачи ------------------------------------

say "7. Конфигурация движка и привязка задачи voice"
bb plugin list 2>/dev/null | grep -qi "$PLUGIN_ID" && ok "плагин загружен" \
  || die "плагин не загрузился — смотрите journalctl -u bb-app -n 40"

bb voice-input config engine groq >/dev/null 2>&1 \
  && ok "engine = groq" || warn "не удалось задать engine (проверьте bb voice-input status)"
bb voice-input config groqKeyEnv GROQ_API_KEY >/dev/null 2>&1 \
  && ok "groqKeyEnv = GROQ_API_KEY" || warn "не удалось задать groqKeyEnv"
[ -f "$HOME/.bb/env.json" ] && ok "ключ GROQ_API_KEY есть в ~/.bb/env.json" \
  || warn "нет ~/.bb/env.json — задайте: bb-app env set GROQ_API_KEY <ключ>"

bb settings ai-services set voice local-voice --plugin "$PLUGIN_ID" >/dev/null 2>&1 \
  && ok "задача voice -> local-voice ($PLUGIN_ID)" || warn "не удалось привязать задачу voice"

say "8. Итог (статус обновляется с задержкой до 10 с)"
sleep 12
bb plugin list 2>/dev/null | grep -i "$PLUGIN_ID" || warn "плагин не найден в bb plugin list"
bb settings show 2>/dev/null | grep -i voiceTranscriptionEnabled || true
echo
bb voice-input status 2>/dev/null
echo
bb settings ai-services 2>/dev/null | head -6
echo
printf 'Проверка записи: откройте https://bb.stvcc.tech, скажите фразу в поле ввода.\n\n'

