# Stage 1: Runtime Base
# Initialize the lightweight runtime environment for ASP.NET Core 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build
# Initialize the .NET 8.0 SDK environment required to compile the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project definition file from the current directory context
COPY ["Planora.Api.csproj", "."]
RUN dotnet restore "./Planora.Api.csproj"

# Copy the remaining source code in the local directory
COPY . .
WORKDIR "/src/."
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