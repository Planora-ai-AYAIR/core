# ── Stage 1: Build ──────────────────────────────────────────────────────────
# Use the full .NET 8 SDK image so we can restore packages and compile the app.
# We name this stage "build" so the next stage can copy from it.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore Planora.sln

RUN dotnet publish src/Planora.API/Planora.API.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────────────────
# Use the lightweight ASP.NET 8 runtime-only image (no SDK = smaller image).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy only the published output from the build stage.
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Planora.API.dll"]