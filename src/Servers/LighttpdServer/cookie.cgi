#!/bin/sh
printf 'Content-Type: text/plain\r\n\r\n'
if [ -n "$HTTP_COOKIE" ]; then
    echo "$HTTP_COOKIE" | tr ';' '\n' | while read -r pair; do
        trimmed=$(echo "$pair" | sed 's/^ *//')
        printf '%s\n' "$trimmed"
    done
fi
