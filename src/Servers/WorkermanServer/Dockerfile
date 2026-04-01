FROM php:8.5-cli

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    unzip \
    libzip-dev \
    && docker-php-ext-install zip \
    && apt-get autoremove -y && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=composer/composer:latest-bin --link /composer /usr/local/bin/composer

WORKDIR /workerman
COPY src/Servers/WorkermanServer/composer.json .
RUN composer install --optimize-autoloader --classmap-authoritative --no-dev --quiet
COPY src/Servers/WorkermanServer/php.ini /etc/php/8.5/cli/php.ini

COPY src/Servers/WorkermanServer/start.php .

EXPOSE 8080

CMD ["php", "/workerman/start.php", "start"]
