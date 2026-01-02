# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TransactionApi.csproj", "./"]
RUN dotnet restore "TransactionApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "TransactionApi.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "TransactionApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published app from publish stage
COPY --from=publish /app/publish .

# Create logs directory with proper permissions
USER root
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "TransactionApi.dll"]

