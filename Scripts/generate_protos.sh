#!/bin/bash

BASE_PROTO_DIR="Assets/1_FeedTheRealm/Core/Common/Protocol/Protobufs"
EVENTS_PROTO_DIR="$BASE_PROTO_DIR/Events"
COMMANDS_PROTO_DIR="$BASE_PROTO_DIR/Commands"

BASE_OUT_DIR="Assets/1_FeedTheRealm/Core/Common/Protocol/RpcMessages"
COMMON_OUT_DIR="$BASE_OUT_DIR/Common"
EVENT_OUT_DIR="$BASE_OUT_DIR/ServerEventContent"
COMMAND_OUT_DIR="$BASE_OUT_DIR/CommandContent"

rm -rf "$COMMON_OUT_DIR"/*
rm -rf "$EVENT_OUT_DIR"/*
rm -rf "$COMMAND_OUT_DIR"/*

protoc --proto_path="$BASE_PROTO_DIR" --csharp_out="$COMMON_OUT_DIR" "$BASE_PROTO_DIR"/*.proto
protoc --proto_path="$BASE_PROTO_DIR" --csharp_out="$EVENT_OUT_DIR" "$EVENTS_PROTO_DIR"/*.proto
protoc --proto_path="$BASE_PROTO_DIR" --csharp_out="$COMMAND_OUT_DIR" "$COMMANDS_PROTO_DIR"/*.proto
