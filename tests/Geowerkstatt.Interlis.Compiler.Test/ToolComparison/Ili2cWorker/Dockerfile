# Image for the ili2c comparison tests: a long-running JVM that compiles INTERLIS models with ili2c on request (see
# Ili2cWorker.java). Built by the tests from the test output folder, which holds this file, the worker source and the
# ili2c.jar that the test project downloads during build.

# Java 17: the worker keeps the JVM alive with a security manager, which Java 24 and later no longer support.
FROM eclipse-temurin:17-jdk AS build
WORKDIR /build
COPY ili2c.jar Ili2cWorker.java ./
RUN javac -cp ili2c.jar -d classes Ili2cWorker.java

FROM eclipse-temurin:17-jre
COPY ili2c.jar /ili2c/
COPY --from=build /build/classes /ili2c/classes
EXPOSE 8080
# One worker runs per parallel test and compiles one model at a time, so a single core and the serial GC suffice.
ENTRYPOINT ["java", "-Djava.security.manager=allow", "-XX:+UseSerialGC", "-XX:ActiveProcessorCount=1", "-cp", "/ili2c/ili2c.jar:/ili2c/classes", "Ili2cWorker"]
