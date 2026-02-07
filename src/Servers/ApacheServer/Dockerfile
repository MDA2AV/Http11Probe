FROM httpd:2.4

COPY src/Servers/ApacheServer/httpd-probe.conf /usr/local/apache2/conf/httpd.conf
RUN echo "OK" > /usr/local/apache2/htdocs/index.html
