# syntax=docker/dockerfile:1
FROM node:24.11.0-alpine AS spa
WORKDIR /spa
COPY drydock.frontend-services/package.json drydock.frontend-services/package-lock.json ./
RUN npm ci
COPY drydock.frontend-services/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY drydock.backend-services/ ./
COPY --from=spa /spa/dist ./Drydock.Api/wwwroot
RUN dotnet publish Drydock.Api/Drydock.Api.csproj -c Release -o /app -p:BuildSpa=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl python3 openssh-client \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app ./
COPY drydock.runner-services/runner.py drydock.runner-services/transport.py /app/runner/
RUN mkdir -p /data/keys /data/deployments && chown -R app:app /data
ENV ASPNETCORE_HTTP_PORTS=8080 DataProtection__KeyPath=/data/keys
USER app
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=4 \
    CMD curl --fail --silent http://localhost:8080/api/system/ready || exit 1
ENTRYPOINT ["dotnet", "Drydock.Api.dll"]
