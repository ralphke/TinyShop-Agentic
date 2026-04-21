# init-db client image for TinyShop
FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive
ENV PATH="/opt/mssql-tools/bin:/opt/sqlpackage:${PATH}"

RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        bash \
        curl \
        gnupg2 \
        apt-transport-https \
        unixodbc-dev \
        ca-certificates \
        lsb-release \
        python3 \
        unzip; \
    curl -sSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /usr/share/keyrings/microsoft.gpg; \
    echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/ubuntu/22.04/prod jammy main" > /etc/apt/sources.list.d/mssql-release.list; \
    apt-get update; \
    ACCEPT_EULA=Y apt-get install -y --no-install-recommends msodbcsql18 mssql-tools; \
    mkdir -p /opt/sqlpackage; \
    curl -L https://aka.ms/sqlpackage-linux -o /tmp/sqlpackage.zip; \
    unzip /tmp/sqlpackage.zip -d /opt/sqlpackage; \
    chmod +x /opt/sqlpackage/sqlpackage; \
    rm -f /tmp/sqlpackage.zip; \
    rm -rf /var/lib/apt/lists/*
