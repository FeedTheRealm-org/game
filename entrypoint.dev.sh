#!/bin/sh

# This entrypoint script is intended for local development.
# It can be used manually or as the entrypoint for a Docker container.
# It expects the server executable as the first argument, followed by any additional arguments.

set -e

if [ $# -eq 0 ]; then
  echo "Usage: $0 <server executable> [args...]"
  exit 1
fi

SERVER_EXECUTABLE=$1
shift

# Load .env if present
if [ -f .env ]; then
    set -a
    . ./.env
    set +a
fi

exec "$SERVER_EXECUTABLE" "$@"
