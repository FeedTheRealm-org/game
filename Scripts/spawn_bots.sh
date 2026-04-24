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

trap "kill 0" SIGINT SIGTERM

echo "Starting $N processes: $CMD"

for ((i=0; i<N; i++)); do
  "$CMD" --world-id=${WORLD_ID} --zone-id=${ZONE_ID} --bot-id=${i} -batchmode -nographics &
done

wait
