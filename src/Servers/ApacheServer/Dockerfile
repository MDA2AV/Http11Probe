FROM httpd:2.4

COPY src/Servers/ApacheServer/httpd-probe.conf /usr/local/apache2/conf/httpd.conf
RUN echo "OK" > /usr/local/apache2/htdocs/index.html
COPY src/Servers/ApacheServer/echo.cgi /usr/local/apache2/cgi-bin/echo.cgi
COPY src/Servers/ApacheServer/cookie.cgi /usr/local/apache2/cgi-bin/cookie.cgi
RUN chmod +x /usr/local/apache2/cgi-bin/echo.cgi /usr/local/apache2/cgi-bin/cookie.cgi
