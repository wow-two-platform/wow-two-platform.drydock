# syntax=docker/dockerfile:1
FROM node:24.11.0-alpine AS spa
WORKDIR /spa
COPY wheelhouse.frontend-services/package.json wheelhouse.frontend-services/package-lock.json ./
RUN npm ci
COPY wheelhouse.frontend-services/ ./
# The build's version from the shared publish workflow (X.Y.Z or a pre-release); unset in a local build. Declared
# after the dependency layers, since a changed build argument misses the cache of every RUN after it.
ARG APP_VERSION
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY wheelhouse.backend-services/ ./
COPY --from=spa /spa/dist ./Wheelhouse.Api/wwwroot
# MSBuild reads APP_VERSION from the environment; Directory.Build.props turns it into the product version.
ARG APP_VERSION
RUN case "$TARGETARCH" in amd64) rid=linux-x64 ;; arm64) rid=linux-arm64 ;; *) exit 1 ;; esac \
    && dotnet publish Wheelhouse.Api/Wheelhouse.Api.csproj -c Release -o /app -r "$rid" --self-contained false -p:BuildSpa=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl python3 openssh-client \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
COPY wheelhouse.runner-services/runner.py wheelhouse.runner-services/transport.py wheelhouse.runner-services/fleet.py wheelhouse.runner-services/artifacts.py /app/runner/
RUN mkdir -p /data/keys /data/deployments && chown -R app:app /data
ENV ASPNETCORE_HTTP_PORTS=8080 DataProtection__KeyPath=/data/keys
USER app
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=4 \
    CMD curl --fail --silent http://localhost:8080/api/system/ready || exit 1
ENTRYPOINT ["dotnet", "Wheelhouse.Api.dll"]
