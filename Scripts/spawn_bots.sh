#!/usr/bin/env bash

set -e

if [ $# -ne 4 ]; then
  echo "Usage: $0 <num_processes> <executable_path> <world_id> <zone_id>"
  exit 1
fi

if [ -z "${ADMIN_TOKEN:-}" ]; then
  cat <<'EOF'
Error: ADMIN_TOKEN environment variable is required.

Example to obtain a token:
export ADMIN_TOKEN=$(curl -X POST http://localhost:8000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.admin","password":"admin123"}' \
  | jq -r '.data.access_token')
EOF
  exit 1
fi

N=$1
CMD="$2"
WORLD_ID=$3
ZONE_ID=$4

PIDS=()

cleanup() {
  echo "Stopping ${#PIDS[@]} processes..."
  for pid in "${PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
      kill "$pid"
    fi
  done
  wait
  echo "All processes stopped."
}

trap cleanup SIGINT SIGTERM

mkdir -p ./bot_logs/

echo "Starting $N processes: $CMD"
for ((i=1; i<=N; i++)); do
  ADMIN_TOKEN="$ADMIN_TOKEN" "$CMD" --world-id=${WORLD_ID} --zone-id=${ZONE_ID} --bot-id=${i} -batchmode -nographics \
      > "./bot_logs/bot_${i}.log" 2>&1 &
  PIDS+=($!)
done
echo "Finished starting bots."

wait
