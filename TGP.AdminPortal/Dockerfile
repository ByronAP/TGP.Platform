# Dockerfile for TGP Admin Portal
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG GH_TOKEN
WORKDIR /src

# Register GitHub Packages for TGP.Data NuGet package
RUN dotnet nuget add source "https://nuget.pkg.github.com/ByronAP/index.json" -n "github" -u "ByronAP" -p "${GH_TOKEN}" --store-password-in-clear-text

# Copy csproj and nuget.config for restore
COPY nuget.config ./
COPY src/TGP.AdminPortal/TGP.AdminPortal.csproj src/TGP.AdminPortal/

# Restore dependencies (TGP.Data comes from NuGet)
RUN dotnet restore src/TGP.AdminPortal/TGP.AdminPortal.csproj

# Copy project source files
COPY src/TGP.AdminPortal/ src/TGP.AdminPortal/

# Build the application
WORKDIR /src/src/TGP.AdminPortal
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Install dependencies for SQL Server (System.Data.SqlClient/Microsoft.Data.SqlClient)
USER root
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

# Use the built-in non-root user
USER $APP_UID

# Expose port
EXPOSE 8080

# Entry point
ENTRYPOINT ["dotnet", "TGP.AdminPortal.dll"]
