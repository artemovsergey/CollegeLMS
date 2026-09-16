#!/bin/bash
# Pull latest code and rebuild on VPS (manual fallback)
# Usage: ./scripts/vps-pull.sh
set -e

cd /home/user1/CollegeLMS

echo "=== git pull ==="
git pull

echo "=== .env check ==="
if [ ! -f .env ]; then
  echo "⚠️  .env missing."
  echo "   Or deploy via GitHub Actions (secrets will fill .env)"
  exit 1
fi

echo "=== Build & start all services ==="
docker compose --profile max-bot up --build -d

echo "=== Health check ==="
sleep 5
curl -s http://localhost:5026/healthz || echo "API not responding"

echo "=== DONE ==="
