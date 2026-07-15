---
name: vps-deploy
description: Configure production deployment on VPS with Nginx reverse proxy, Docker Compose, and GitHub Actions CI/CD
---

# vps-deploy

> **Note**: Deploy workflow was removed for MVP. This skill is a placeholder for future use.

Configure VPS deployment with Nginx, Docker Compose, and GitHub Actions.

## Infrastructure

- VPS with Docker + Docker Compose
- Nginx reverse proxy (HTTP → API/Frontend containers)
- GitHub Actions deploy.yml (push to master → deploy)

## Required secrets

| Secret | Purpose |
|--------|---------|
| `VPS_HOST` | VPS IP or domain |
| `VPS_USERNAME` | SSH user |
| `SSH_PRIVATE_KEY` | Private SSH key for deploy |
| `GITHUB_TOKEN` | GitHub API token |

## Future setup steps

1. Create `deploy.yml` GitHub Actions workflow
2. Configure loadbalancer production config
3. Set up SSL with Let's Encrypt
4. Add health check monitoring
