# ── Stage 1: Build ──────────────────────────────────────────────────────────
# Use the full .NET 8 SDK image so we can restore packages and compile the app.
# We name this stage "build" so the next stage can copy from it.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire repo context (from apps/api-dotnet/) into /src.
# This includes the .sln and all project folders under src/ and tests/.
COPY . .

# Restore NuGet packages for the whole solution.
RUN dotnet restore Planora.sln

# Publish the API project in Release mode.
# Output goes to /app/publish — this is what the runtime stage will use.
RUN dotnet publish src/Planora.API/Planora.API.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────────────────
# Use the lightweight ASP.NET 8 runtime-only image (no SDK = smaller image).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy only the published output from the build stage.
COPY --from=build /app/publish .

# Tell ASP.NET Core to listen on port 5000 inside the container.
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Planora.API.dll"]
