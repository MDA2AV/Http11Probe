FROM alpine:3.20
RUN apk add --no-cache lighttpd
COPY src/Servers/LighttpdServer/lighttpd.conf /etc/lighttpd/lighttpd.conf
COPY src/Servers/LighttpdServer/index.cgi /var/www/index.cgi
RUN chmod +x /var/www/index.cgi
EXPOSE 8080
CMD ["lighttpd", "-D", "-f", "/etc/lighttpd/lighttpd.conf"]
