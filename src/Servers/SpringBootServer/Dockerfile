FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY src/Servers/SpringBootServer/pom.xml .
RUN mvn dependency:go-offline -q
COPY src/Servers/SpringBootServer/src/ src/
RUN mvn package -q -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /src/target/*.jar app.jar
ENTRYPOINT ["java", "-jar", "app.jar", "--server.port=9004", "--server.address=127.0.0.1"]
