#!/usr/bin/env bash

# Exit immediately if a command exits with a non-zero status
set -e

# Cleanup child processes on exit (Ctrl+C)
trap 'echo -e "\n🛑 Stopping MotelLease services..."; kill $(jobs -p) 2>/dev/null || true; exit 0' SIGINT SIGTERM EXIT

echo "=========================================================="
echo "🏨 MotelLease - Development Environment Runner"
echo "=========================================================="

# 1. Check if database container is running
if ! docker ps --format '{{.Names}}' | grep -q 'motellease-db'; then
  echo "📦 Starting PostgreSQL (PostGIS) container..."
  docker compose up -d db
fi

# 2. Start Backend API with Hot Reload
echo "🚀 Starting ASP.NET Core API on http://localhost:5004..."
dotnet watch --project backend/MotelLease.Api &
BACKEND_PID=$!

# Wait for backend port to be open
echo "⏳ Waiting for API to become available..."
until curl -s http://localhost:5004/health > /dev/null 2>&1 || [ ! -e /proc/$BACKEND_PID ]; do
  sleep 1
done

# 3. Start Frontend Nuxt 4 with HMR
echo "✨ Starting Nuxt 4 Frontend on http://localhost:3000..."
npm --prefix frontend run dev

# Wait for background jobs
wait
