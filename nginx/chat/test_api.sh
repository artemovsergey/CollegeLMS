# Test script for OpenCode API - run on VPS
set -e

echo "=== Test 1: SPA session creation ==="
curl -s -X POST http://127.0.0.1:4096/api/session \
  -H 'Content-Type: application/json' \
  -d '{"model":{"id":"deepseek-v4-flash-free","providerID":"opencode"},"agent":"ask"}'
echo ""

echo "=== Test 2: SPA session creation through nginx ==="
curl -s -X POST http://localhost/api/session \
  -H 'Content-Type: application/json' \
  -d '{"model":{"id":"deepseek-v4-flash-free","providerID":"opencode"},"agent":"ask"}'
echo ""

echo "=== Test 3: Internal session ==="
SID=$(curl -s -X POST http://127.0.0.1:4096/session \
  -H 'Content-Type: application/json' \
  -d '{}' | python3 -c "import json,sys;print(json.load(sys.stdin).get('id',''))")
echo "SID=$SID"

echo "=== Test 4: Send message (internal API) ==="
curl -s -X POST "http://127.0.0.1:4096/session/$SID/message" \
  -H 'Content-Type: application/json' \
  -d '{"parts":[{"type":"text","text":"hi"}]}'
echo ""

echo "=== Test 5: Send message through nginx ==="
SID2=$(curl -s -X POST http://localhost/session \
  -H 'Content-Type: application/json' \
  -d '{}' | python3 -c "import json,sys;print(json.load(sys.stdin).get('id',''))")
echo "SID2=$SID2"
curl -s -X POST "http://localhost/session/$SID2/message" \
  -H 'Content-Type: application/json' \
  -d '{"parts":[{"type":"text","text":"hi"}]}'
echo ""

echo "=== Test 6: Chat page ==="
curl -s -o /dev/null -w "%{http_code}" http://localhost/chat/
echo ""
