# GenApp API image. The web interface is served by the API (src/GenApp.Api/wwwroot, built assets are committed),
# so one image covers both. Stages:
#   build    .NET SDK: publishes the API and bundles the EF Core migrations
#   migrate  runs the migration bundle once and exits (docker-compose.yml starts the API after it)
#   api      the default target: only the published API on the ASP.NET Core runtime

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer is reused until a package reference changes.
COPY global.json dotnet-tools.json ./
COPY src/GenApp.Domain/GenApp.Domain.csproj src/GenApp.Domain/
COPY src/GenApp.Application/GenApp.Application.csproj src/GenApp.Application/
COPY src/GenApp.Infrastructure/GenApp.Infrastructure.csproj src/GenApp.Infrastructure/
COPY src/GenApp.Api/GenApp.Api.csproj src/GenApp.Api/
RUN dotnet restore src/GenApp.Api/GenApp.Api.csproj \
    && dotnet tool restore

COPY src/ src/
RUN dotnet publish src/GenApp.Api/GenApp.Api.csproj --configuration Release --no-restore --output /app

# Creating the bundle starts Program.cs, and AddInfrastructure requires a connection string. The bundle does not
# connect while it is built; the real connection string is given when it runs (docker-compose.yml).
RUN ConnectionStrings__GenApp="Host=build-only" \
    dotnet ef migrations bundle \
        --project src/GenApp.Infrastructure \
        --startup-project src/GenApp.Api \
        --configuration Release \
        --output /bundle/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
WORKDIR /bundle
COPY --from=build /bundle/efbundle .
USER $APP_UID
# Shell form so that the connection string comes from the environment. Migrations already applied are skipped.
ENTRYPOINT ["/bin/sh", "-c", "exec ./efbundle --connection \"$ConnectionStrings__GenApp\""]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /app .
# Non-root user of the Microsoft images; ASP.NET Core listens on 8080 in these images.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GenApp.Api.dll"]
