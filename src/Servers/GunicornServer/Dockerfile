FROM python:3.13-slim
RUN pip install --no-cache-dir gunicorn
COPY src/Servers/GunicornServer/app.py /app/app.py
WORKDIR /app
EXPOSE 8080
CMD ["gunicorn", "-b", "0.0.0.0:8080", "app:app"]
