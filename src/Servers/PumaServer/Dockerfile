FROM ruby:3.3-slim
RUN gem install puma --no-doc
COPY src/Servers/PumaServer/config.ru /app/config.ru
WORKDIR /app
EXPOSE 8080
CMD ["puma", "-b", "tcp://0.0.0.0:8080"]
