#!/bin/bash
# Deploy to VPS via SSH
# Usage: ./scripts/deploy.sh <vps-host> [ssh-user]
set -e

VPS_HOST="${1:?Usage: ./scripts/deploy.sh <vps-host> [ssh-user]}"
SSH_USER="${2:-opencode}"
REMOTE_DIR="/home/$SSH_USER/CollegeLMS"

echo "=== Push to origin ==="
git push origin master

echo "=== Deploy to $VPS_HOST ==="
ssh "$SSH_USER@$VPS_HOST" "cd $REMOTE_DIR && ./scripts/vps-pull.sh"

echo "=== DONE ==="
