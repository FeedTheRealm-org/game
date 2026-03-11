#!/bin/bash

PROTO_DIR="Assets/1_FeedTheRealm/Core/Common/Protocol/Protobufs"
OUT_DIR="Assets/1_FeedTheRealm/Core/Common/Protocol/RpcMessages/ServerEventContent"

protoc --proto_path="$PROTO_DIR" --csharp_out="$OUT_DIR" "$PROTO_DIR"/*.proto
