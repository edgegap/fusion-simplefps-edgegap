# Simple FPS (Photon Fusion) dedicated server image for Edgegap.
# Build context: the Unity project root (see .dockerignore).
FROM ubuntu:22.04

ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl \
 && rm -rf /var/lib/apt/lists/* \
 && useradd --system --uid 1001 --create-home --home-dir /home/server server

COPY Builds/EdgegapServer/ /srv/simplefps/
COPY edgegap/start-server.sh /usr/local/bin/start-server

RUN sed -i 's/\r$//' /usr/local/bin/start-server \
 && chmod 0755 /usr/local/bin/start-server /srv/simplefps/ServerBuild

USER server
WORKDIR /home/server
EXPOSE 7777/udp
STOPSIGNAL SIGTERM
ENTRYPOINT ["/usr/local/bin/start-server"]
