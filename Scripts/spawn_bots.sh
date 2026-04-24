#!/usr/bin/env bash

set -e

if [ $# -ne 4 ]; then
  echo "Usage: $0 <num_processes> <executable_path> <world_id> <zone_id>"
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

echo "Starting $N processes: $CMD"
for ((i=0; i<N; i++)); do
  "$CMD" --world-id=${WORLD_ID} --zone-id=${ZONE_ID} --bot-id=${i} -batchmode -nographics &
  PIDS+=($!)
done

wait
