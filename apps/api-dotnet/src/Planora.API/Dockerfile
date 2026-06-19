# Stage 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Planora.API/Planora.Api.csproj", "Planora.API/"]
COPY ["Planora.Application/Planora.Application.csproj", "Planora.Application/"]
COPY ["Planora.Infrastructure/Planora.Infrastructure.csproj", "Planora.Infrastructure/"]
COPY ["Planora.Domain/Planora.Domain.csproj", "Planora.Domain/"]
RUN dotnet restore "Planora.API/Planora.Api.csproj"
COPY . .
WORKDIR "/src/Planora.API"
RUN dotnet build "Planora.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 2 — Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Planora.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 3 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
USER $APP_UID
COPY --from=publish /app/publish .
# Fixed: DLL name matches Planora.Api.csproj — was incorrectly "Planora.dll"
ENTRYPOINT ["dotnet", "Planora.Api.dll"]
