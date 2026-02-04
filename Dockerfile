# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Arriba.sln .
COPY src/Arriba.Core/Arriba.Core.csproj src/Arriba.Core/
COPY src/Arriba.Web/Arriba.Web.csproj src/Arriba.Web/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/Arriba.Web/Arriba.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser
USER appuser

# Copy published application
COPY --from=build /app/publish .

# Expose port (Render uses PORT env var, default 10000)
EXPOSE 10000

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production

# Use shell form to allow PORT environment variable substitution
CMD ASPNETCORE_URLS=http://+:${PORT:-10000} dotnet Arriba.Web.dll
