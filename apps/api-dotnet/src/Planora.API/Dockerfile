# Stage 1: Runtime Base
# Initialize the lightweight runtime environment for ASP.NET Core 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build with full context
# Initialize the .NET 8.0 SDK environment required to compile the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 1. Copy all project definition (.csproj) files to leverage Docker layer caching for NuGet dependencies
COPY ["apps/api-dotnet/src/Planora.API/Planora.Api.csproj", "apps/api-dotnet/src/Planora.API/"]
COPY ["apps/api-dotnet/src/Planora.Application/Planora.Application.csproj", "apps/api-dotnet/src/Planora.Application/"]
COPY ["apps/api-dotnet/src/Planora.Domain/Planora.Domain.csproj", "apps/api-dotnet/src/Planora.Domain/"]
COPY ["apps/api-dotnet/src/Planora.Infrastructure/Planora.Infrastructure.csproj", "apps/api-dotnet/src/Planora.Infrastructure/"]

# 2. Restore NuGet dependencies based on the primary API project entry point
RUN dotnet restore "apps/api-dotnet/src/Planora.API/Planora.Api.csproj"

# 3. Copy the remaining source code and build the API project using the defined configuration
COPY . .
WORKDIR "/src/apps/api-dotnet/src/Planora.API"
RUN dotnet build "Planora.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
# Compile and optimize the application artifacts for production deployment
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Planora.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
# Copy the compiled binaries into the clean, minimal runtime base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Planora.Api.dll"]