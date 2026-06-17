# Multi-stage build for the Sprint Planner API (ASP.NET Core 8 + OR-Tools).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first for better layer caching.
COPY global.json Directory.Build.props ./
COPY SprintPlanner.sln ./
COPY src/SprintPlanner.Domain/SprintPlanner.Domain.csproj src/SprintPlanner.Domain/
COPY src/SprintPlanner.Application/SprintPlanner.Application.csproj src/SprintPlanner.Application/
COPY src/SprintPlanner.Infrastructure/SprintPlanner.Infrastructure.csproj src/SprintPlanner.Infrastructure/
COPY src/SprintPlanner.Optimization/SprintPlanner.Optimization.csproj src/SprintPlanner.Optimization/
COPY src/SprintPlanner.Api/SprintPlanner.Api.csproj src/SprintPlanner.Api/
RUN dotnet restore src/SprintPlanner.Api/SprintPlanner.Api.csproj

COPY . .
RUN dotnet publish src/SprintPlanner.Api/SprintPlanner.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# OR-Tools' native libraries need libgomp at runtime.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "SprintPlanner.Api.dll"]
