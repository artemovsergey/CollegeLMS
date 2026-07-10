#!/bin/bash
# Pull latest code and rebuild on VPS
set -e

cd /home/opencode/CollegeLMS

echo "=== git pull ==="
git pull

echo "=== Rebuild agentbridge ==="
docker compose build --no-cache agentbridge

echo "=== Restart services ==="
docker compose up -d

echo "=== Health check ==="
sleep 3
curl -s http://localhost:4096/global/health || echo "OpenCode not responding"
curl -s http://localhost:5030/health || echo "AgentBridge not responding"

echo "=== DONE ==="
