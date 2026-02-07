FROM python:3.12-slim
WORKDIR /app
RUN pip install --no-cache-dir flask
COPY src/Servers/FlaskServer/app.py .
ENTRYPOINT ["python3", "app.py", "9002"]
