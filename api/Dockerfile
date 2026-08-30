# Builds two apps from this one image: LawPortal.Api (the long-running web process) and
# LawPortal.DbMigrator (a one-shot process run as Railway's pre-deploy command, see
# railway.json). Both publish into the same runtime image so the pre-deploy command has
# everything it needs without a second build.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so `dotnet restore` is cached across builds that only change source,
# not dependencies.
COPY src/LawPortal.Domain/LawPortal.Domain.csproj src/LawPortal.Domain/
COPY src/LawPortal.Application/LawPortal.Application.csproj src/LawPortal.Application/
COPY src/LawPortal.Infrastructure/LawPortal.Infrastructure.csproj src/LawPortal.Infrastructure/
COPY src/LawPortal.Api/LawPortal.Api.csproj src/LawPortal.Api/
COPY src/LawPortal.DbMigrator/LawPortal.DbMigrator.csproj src/LawPortal.DbMigrator/
RUN dotnet restore src/LawPortal.Api/LawPortal.Api.csproj && \
    dotnet restore src/LawPortal.DbMigrator/LawPortal.DbMigrator.csproj

COPY src/ src/
RUN dotnet publish src/LawPortal.Api/LawPortal.Api.csproj -c Release -o /app/api --no-restore && \
    dotnet publish src/LawPortal.DbMigrator/LawPortal.DbMigrator.csproj -c Release -o /app/migrator --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/api ./api
COPY --from=build /app/migrator ./migrator

# Railway assigns a container port dynamically via $PORT — ASPNETCORE_URLS must be set at
# container start (shell form, not a build-time ENV) so it picks up the actual runtime value.
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet /app/api/LawPortal.Api.dll"]
