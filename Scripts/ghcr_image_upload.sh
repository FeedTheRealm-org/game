#!/bin/bash
# Example usage: ./Scripts/ghcr_image_upload.sh
set -eu

read -r PAT || true
echo "$PAT" | docker login ghcr.io -u atusgames --password-stdin

echo "Building and pushing ghcr.io/feedtherealm-org/ftr-server:latest"
docker build -t ghcr.io/feedtherealm-org/ftr-server:latest .
docker push ghcr.io/feedtherealm-org/ftr-server:latest

rm $HOME/.docker/config.json
