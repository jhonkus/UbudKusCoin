#!/bin/sh
set -eu

: "${TRUST_HEIGHT:?TRUST_HEIGHT is required}"
: "${TRUST_HASH:?TRUST_HASH is required}"
: "${RPC_SERVERS:?RPC_SERVERS is required}"
: "${NODE_HOME:?NODE_HOME is required}"

config="/network/${NODE_HOME}/config/config.toml"

sed -i '/^\[statesync\]/,/^\[/ s/^enable = .*/enable = true/' "$config"
sed -i "s|^rpc_servers =.*|rpc_servers = \"$RPC_SERVERS\"|" "$config"
sed -i "s|^trust_height =.*|trust_height = $TRUST_HEIGHT|" "$config"
sed -i "s|^trust_hash =.*|trust_hash = \"$TRUST_HASH\"|" "$config"
sed -i 's|^trust_period =.*|trust_period = "168h0m0s"|' "$config"

grep -A12 '^\[statesync\]' "$config"
