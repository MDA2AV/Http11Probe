#!/bin/sh
printf 'Content-Type: text/plain\r\n\r\n'
if [ "$REQUEST_METHOD" = "POST" ]; then
    cat
else
    printf 'OK'
fi
