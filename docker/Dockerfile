FROM mcr.microsoft.com/powershell:latest as env-build
LABEL version "1.0"
LABEL VENDOR YouPark AS
LABEL description "A docker image for building snapx"
  
ENV DEBIAN_FRONTEND noninteractive

RUN \
  apt-get update && \
  apt-get install -y --no-install-recommends \
    coreutils wget apt-utils apt-transport-https ca-certificates software-properties-common cmake \
    make gcc g++ \
    mingw-w64 g++-mingw-w64-x86-64 g++-mingw-w64-i686 upx-ucl \
    # native c++ build dependencies
    uuid-dev

RUN \
  wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb \
  && dpkg -i packages-microsoft-prod.deb \
  && add-apt-repository universe \
  && apt-get update && apt-get install -y --no-install-recommends dotnet-sdk-2.2

CMD ["/build/snapx/docker/entry.sh"]