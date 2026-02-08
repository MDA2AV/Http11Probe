FROM ubuntu:24.04 AS build
RUN apt-get update && apt-get install -y cmake gcc g++ pkg-config libssl-dev zlib1g-dev git && rm -rf /var/lib/apt/lists/*
RUN git clone --recurse-submodules --depth 1 https://github.com/h2o/h2o.git /src/h2o
WORKDIR /src/h2o/build
RUN cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=/usr/local && make -j$(nproc) && make install

FROM ubuntu:24.04
RUN apt-get update && apt-get install -y libssl3t64 && rm -rf /var/lib/apt/lists/*
COPY --from=build /usr/local/bin/h2o /usr/local/bin/
COPY --from=build /usr/local/share/h2o/ /usr/local/share/h2o/
COPY src/Servers/H2OServer/h2o.conf /etc/h2o/h2o.conf
RUN mkdir -p /var/www && echo "OK" > /var/www/index.html
ENTRYPOINT ["h2o", "-c", "/etc/h2o/h2o.conf"]
