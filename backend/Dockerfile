# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY MotelLease.slnx ./
COPY MotelLease.Domain/*.csproj ./MotelLease.Domain/
COPY MotelLease.Application/*.csproj ./MotelLease.Application/
COPY MotelLease.Infrastructure/*.csproj ./MotelLease.Infrastructure/
COPY MotelLease.Api/*.csproj ./MotelLease.Api/
COPY MotelLease.Tests/*.csproj ./MotelLease.Tests/

# Restore dependencies
RUN dotnet restore MotelLease.slnx

# Copy full source code and build
COPY . .
WORKDIR /src/MotelLease.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5004
ENV ASPNETCORE_URLS=http://+:5004

ENTRYPOINT ["dotnet", "MotelLease.Api.dll"]
