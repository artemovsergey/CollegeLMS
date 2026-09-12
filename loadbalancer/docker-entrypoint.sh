#!/bin/sh
set -eu

certificate_dir="/etc/letsencrypt/live/stvcc.tech"

if [ ! -s "${certificate_dir}/fullchain.pem" ] || [ ! -s "${certificate_dir}/privkey.pem" ]; then
    mkdir -p "${certificate_dir}"
    openssl req -x509 -nodes -newkey rsa:2048 \
        -days 1 \
        -keyout "${certificate_dir}/privkey.pem" \
        -out "${certificate_dir}/fullchain.pem" \
        -subj "/CN=stvcc.tech" \
        >/dev/null 2>&1
fi

exec nginx -g "daemon off;"
