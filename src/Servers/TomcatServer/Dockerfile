FROM tomcat:11-jre21-temurin
RUN rm -rf /usr/local/tomcat/webapps/*
COPY src/Servers/TomcatServer/webapp/ /usr/local/tomcat/webapps/ROOT/
EXPOSE 8080
CMD ["catalina.sh", "run"]
