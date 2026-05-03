#!/bin/sh
set -e

SERVER_FIXED_TOKEN=$(aws ssm get-parameter \
  --name "/ftr-server/SERVER_FIXED_TOKEN" \
  --with-decryption \
  --query "Parameter.Value" \
  --output text)
[ -z "$SERVER_FIXED_TOKEN" ] && echo "Missing SERVER_FIXED_TOKEN" && exit 1
export SERVER_FIXED_TOKEN

MONGO_CONNECTION_STRING=$(aws ssm get-parameter \
  --name "/ftr-server/MONGO_CONNECTION_STRING" \
  --with-decryption \
  --query "Parameter.Value" \
  --output text)
[ -z "$MONGO_CONNECTION_STRING" ] && echo "Missing MONGO_CONNECTION_STRING" && exit 1
export MONGO_CONNECTION_STRING

exec ./server.x86_64 "$@"
