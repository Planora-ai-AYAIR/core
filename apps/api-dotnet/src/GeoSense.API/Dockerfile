# ── Stage 1: Build ──────────────────────────────────────────────────────────
# Use the full .NET 8 SDK image so we can restore packages and compile the app.
# We name this stage "build" so the next stage can copy from it.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire repo context (from apps/api-dotnet/) into /src.
# This includes the .sln and all project folders under src/ and tests/.
COPY . .

# Restore NuGet packages for the whole solution.
# Using the .sln ensures all projects are restored in one pass.
RUN dotnet restore GeoSense.sln

# Publish the API project in Release mode.
# Output goes to /app/publish — this is what the runtime stage will use.
RUN dotnet publish src/GeoSense.API/GeoSense.API.csproj -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────────────────
# Use the lightweight ASP.NET 8 runtime-only image (no SDK = smaller image).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy only the published output from the build stage.
# This keeps the final image clean — no source code, no SDK, no build artifacts.
COPY --from=build /app/publish .

# Tell ASP.NET Core to listen on port 5000 inside the container.
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# The entry point is the compiled Planora API dll.
# The dll name still reflects the old source name until a code-rename PR lands.
ENTRYPOINT ["dotnet", "GeoSense.API.dll"]
