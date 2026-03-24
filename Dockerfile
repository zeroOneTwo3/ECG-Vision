# 1. BASE: Runtime + Python (Updated to .NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Install Python for signal processing
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/*

# Install your ECG signal libraries
RUN pip3 install --no-cache-dir --break-system-packages wfdb numpy pandas

# 2. BUILD: (Updated to .NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects from the src directory
COPY ["src/EcgVision.Web/EcgVision.Web.csproj", "EcgVision.Web/"]
COPY ["src/EcgVision.Infrastructure/EcgVision.Infrastructure.csproj", "EcgVision.Infrastructure/"]
COPY ["src/EcgVision.Core/EcgVision.Core.csproj", "EcgVision.Core/"]

# Restore using the Web project as the entry point
RUN dotnet restore "EcgVision.Web/EcgVision.Web.csproj"

# Copy the entire solution context
COPY . .

# Build from the Web project directory
WORKDIR "/src/src/EcgVision.Web"
RUN dotnet build "EcgVision.Web.csproj" -c Release -o /app/build

# 3. PUBLISH
FROM build AS publish
RUN dotnet publish "EcgVision.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Setup the Python script location
RUN mkdir -p /app/Scripts
COPY src/EcgVision.Web/Scripts/extract_leads.py /app/Scripts/

ENTRYPOINT ["dotnet", "EcgVision.Web.dll"]
