FROM php:8.3-cli
COPY src/Servers/PhpServer/index.php /app/index.php
WORKDIR /app
EXPOSE 8080
CMD ["php", "-S", "0.0.0.0:8080", "index.php"]
