FROM alpine:3.20
RUN apk add --no-cache lighttpd
COPY src/Servers/LighttpdServer/lighttpd.conf /etc/lighttpd/lighttpd.conf
COPY src/Servers/LighttpdServer/index.cgi /var/www/index.cgi
COPY src/Servers/LighttpdServer/echo.cgi /var/www/echo.cgi
COPY src/Servers/LighttpdServer/cookie.cgi /var/www/cookie.cgi
RUN chmod +x /var/www/index.cgi /var/www/echo.cgi /var/www/cookie.cgi
EXPOSE 8080
CMD ["lighttpd", "-D", "-f", "/etc/lighttpd/lighttpd.conf"]
