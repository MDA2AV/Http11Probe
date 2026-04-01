FROM php:8.5-cli

COPY --from=composer/composer:latest-bin --link /composer /usr/local/bin/composer

RUN docker-php-ext-install pcntl > /dev/null
RUN apt-get update -yqq && apt-get install -yqq git unzip > /dev/null && rm -rf /var/lib/apt/lists/*

WORKDIR /workerman
COPY src/Servers/WorkermanServer/composer.json .
RUN composer install --optimize-autoloader --classmap-authoritative --no-dev --quiet
COPY src/Servers/WorkermanServer/php.ini /etc/php/8.5/cli/php.ini

COPY src/Servers/WorkermanServer/start.php .

EXPOSE 8080

CMD ["php", "/workerman/start.php", "start"]
