FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY src/Servers/QuarkusServer/pom.xml .
RUN mvn dependency:go-offline -q
COPY src/Servers/QuarkusServer/src/ src/
RUN mvn package -q -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /src/target/quarkus-app/ quarkus-app/
ENTRYPOINT ["java", "-Dquarkus.http.port=9005", "-jar", "quarkus-app/quarkus-run.jar"]
