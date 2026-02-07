FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY src/Servers/JettyServer/pom.xml .
RUN mvn dependency:go-offline -q
COPY src/Servers/JettyServer/src/ src/
RUN mvn package -q -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /src/target/jetty-server-1.0.0.jar app.jar
ENTRYPOINT ["java", "-jar", "app.jar", "9007"]
