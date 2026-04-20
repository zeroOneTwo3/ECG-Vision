# 1. BASE: Runtime + Python (Using .NET 10 LTS)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Install Python and essential build tools for C-based libs like numpy
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    && rm -rf /var/lib/apt/lists/*

# Use a Virtual Env instead of --break-system-packages
# This avoids potential conflicts with system-level Linux Python scripts
ENV VIRTUAL_ENV=/opt/venv
RUN python3 -m venv $VIRTUAL_ENV
ENV PATH="$VIRTUAL_ENV/bin:$PATH"

# Install ECG signal libraries into the venv
RUN pip3 install --no-cache-dir wfdb numpy pandas

# Create group/user for security
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup

# 2. BUILD: (.NET 10 SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects with better cache utilization
COPY ["src/EcgVision.Web/EcgVision.Web.csproj", "EcgVision.Web/"]
COPY ["src/EcgVision.Infrastructure/EcgVision.Infrastructure.csproj", "EcgVision.Infrastructure/"]
COPY ["src/EcgVision.Core/EcgVision.Core.csproj", "EcgVision.Core/"]

RUN dotnet restore "EcgVision.Web/EcgVision.Web.csproj"
COPY . .

WORKDIR "/src/src/EcgVision.Web"
RUN dotnet build "EcgVision.Web.csproj" -c Release -o /app/build

# 3. PUBLISH
FROM build AS publish
RUN dotnet publish "EcgVision.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. FINAL
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Setup Python scripts and fix permissions in one go
RUN mkdir -p /app/Scripts
COPY src/EcgVision.Web/Scripts/extract_leads.py /app/Scripts/
RUN chown -R appuser:appgroup /app

# Final security step: Switch to non-root
USER appuser

ENTRYPOINT ["dotnet", "EcgVision.Web.dll"]