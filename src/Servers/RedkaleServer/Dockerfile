FROM maven:3.9-eclipse-temurin-21 AS build
WORKDIR /src
COPY src/Servers/RedkaleServer/pom.xml .
RUN mvn dependency:go-offline -q
COPY src/Servers/RedkaleServer/src/ src/
RUN mvn package -q -DskipTests

FROM eclipse-temurin:21-jre
WORKDIR /app
COPY --from=build /src/target/*.jar app.jar
COPY --from=build /src/target/lib/ lib/
ENTRYPOINT ["java", "-cp", "app.jar:lib/*", "probe.Main"]
