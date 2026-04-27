# Example usage: cat .env | ./Scripts/ghcr_upload_item.sh
set -eu

read -r PAT
echo "$PAT" | docker login ghcr.io -u maxogod --password-stdin

docker build -t ghcr.io/feedtherealm-org/ftr-server:latest .
docker push ghcr.io/feedtherealm-org/ftr-server:latest

rm $HOME/.docker/config.json
