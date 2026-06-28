#!/bin/bash
set -e

echo "Pulling latest code..."
cd /home/deploy/collegelms
git pull origin master

echo "Building and starting containers..."
docker compose -f docker-compose.prod.yml down
docker compose -f docker-compose.prod.yml up -d --build

echo "Running migrations..."
docker compose -f docker-compose.prod.yml exec -T api dotnet ef database update

echo "Deployment complete"
docker compose -f docker-compose.prod.yml ps
