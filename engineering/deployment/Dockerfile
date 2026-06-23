# syntax=docker/dockerfile:1

# Multi-stage build for Drydock: ONE image serving the .NET API + the React dashboard.
# The build context MUST be engineering/codebase/, which contains BOTH drydock.backend-services
# and drydock.frontend-services.
#
#   docker build -f engineering/deployment/Dockerfile -t drydock engineering/codebase
#
# Run ONE instance. NEVER expose it publicly — bind to loopback and reach it over Tailscale
# or an SSH tunnel. On first run the app applies EF migrations and creates /data/drydock.db.
#
#   docker run -d --name drydock -p 127.0.0.1:8210:8080 -v drydock-data:/data drydock

# ---- Stage 1: build the React dashboard ----
FROM node:22-alpine AS spa
WORKDIR /spa
COPY drydock.frontend-services/package.json drydock.frontend-services/package-lock.json ./
RUN npm ci
COPY drydock.frontend-services/ ./
RUN npm run build                          # emits /spa/dist

# ---- Stage 2: publish the .NET API (the SPA arrives from stage 1) ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY drydock.backend-services/ ./
# The built SPA lands in wwwroot so `dotnet publish` bundles it into the output.
COPY --from=spa /spa/dist ./Drydock.Api/wwwroot
RUN dotnet publish Drydock.Api/Drydock.Api.csproj -c Release -o /app -p:SkipSpaBuild=true

# ---- Stage 3: runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

# SQLite control-plane store on a volume so data survives container replacement.
ENV ConnectionStrings__Drydock="Data Source=/data/drydock.db"
RUN mkdir -p /data && chown app:app /data
VOLUME ["/data"]

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Drydock.Api.dll"]
