# ============================================
# STAGE 1: Build the application
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (layer caching)
COPY ["RefactorHeatAlertPostGre.csproj", "./"]
RUN dotnet restore "RefactorHeatAlertPostGre.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "RefactorHeatAlertPostGre.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install timezone data for Philippine Time
RUN apt-get update && apt-get install -y tzdata && \
    ln -fs /usr/share/zoneinfo/Asia/Manila /etc/localtime && \
    dpkg-reconfigure -f noninteractive tzdata && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && \
    mkdir -p /app/sharedresource && \
    chown -R appuser:appuser /app

# Copy published output from build stage
COPY --from=build --chown=appuser:appuser /app/publish .

# Copy GeoJSON file (if not embedded in build output)
COPY --chown=appuser:appuser ["sharedresource/talisaycitycebu.json", "/app/sharedresource/"]

# Switch to non-root user
USER appuser

# Use PORT environment variable (Render sets this)
ENV ASPNETCORE_URLS=http://+:${PORT:-80}
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RefactorHeatAlertPostGre.dll"]