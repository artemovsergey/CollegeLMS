#!/bin/bash
# VPS initial setup — Ubuntu 22.04/24.04
# Run as root or with sudo
set -e

OPENCODE_USER="user1"
REPO_URL="git@github.com:artemovsergey/CollegeLMS.git"
PROJECT_DIR="/home/$OPENCODE_USER/CollegeLMS"

echo "=== 1. System packages ==="
apt update && apt upgrade -y
apt install -y curl git ufw

echo "=== 2. Swap (2GB) ==="
if ! swapon --show | grep -q '/swapfile'; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

echo "=== 3. Docker ==="
if ! command -v docker &> /dev/null; then
  curl -fsSL https://get.docker.com | sh
  systemctl enable --now docker
fi

echo "=== 4. Docker Compose plugin ==="
if ! docker compose version &> /dev/null; then
  apt install -y docker-compose-plugin
fi

echo "=== 5. Docker group ==="
if ! groups "$OPENCODE_USER" | grep -q docker; then
  usermod -aG docker "$OPENCODE_USER"
  echo "--- Re-login or run 'newgrp docker' after script ---"
fi

echo "=== 6. Install OpenCode ==="
su - "$OPENCODE_USER" -c 'curl -fsSL https://opencode.ai/install | sh'

echo "=== 7. Clone repo ==="
su - "$OPENCODE_USER" -c "
  if [ ! -d '$PROJECT_DIR' ]; then
    git clone '$REPO_URL' '$PROJECT_DIR'
  fi
"

echo "=== 8. Create .env ==="
if [ ! -f "$PROJECT_DIR/.env" ]; then
  cp "$PROJECT_DIR/.env.example" "$PROJECT_DIR/.env"
  echo "--- Edit $PROJECT_DIR/.env with your values ---"
  echo "--- Press Enter when done ---"
  read
fi

echo "=== 9. Install OpenCode systemd service ==="
cat > /etc/systemd/system/opencode.service <<EOF
[Unit]
Description=OpenCode Serve
After=network.target

[Service]
Type=simple
User=$OPENCODE_USER
WorkingDirectory=$PROJECT_DIR
ExecStart=/home/$OPENCODE_USER/.local/bin/opencode serve --port 4096
Restart=always
RestartSec=5
Environment=HOME=/home/$OPENCODE_USER

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now opencode.service

echo "=== 10. Firewall ==="
ufw allow OpenSSH
ufw allow 80/tcp
ufw --force enable

echo "=== 11. Start services ==="
su - "$OPENCODE_USER" -c "cd $PROJECT_DIR && docker compose up -d --build"

echo ""
echo "=== DONE ==="
echo "OpenCode:  http://localhost:4096"
echo "Nginx:     http://$(curl -s ifconfig.me)"
echo "Logs:      docker compose logs -f"
