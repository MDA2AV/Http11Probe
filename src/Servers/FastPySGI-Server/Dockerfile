FROM python:3.12-slim
WORKDIR /app
RUN pip install --no-cache-dir fastpysgi
COPY src/Servers/FastPySGI-Server/app.py .
ENTRYPOINT ["python3", "app.py", "8080"]
