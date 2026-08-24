# Multi-stage Dockerfile for T&JPork E-Commerce Application

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/TJPork.Core/TJPork.Core.csproj", "src/TJPork.Core/"]
COPY ["src/TJPork.Infrastructure/TJPork.Infrastructure.csproj", "src/TJPork.Infrastructure/"]
COPY ["src/TJPork.Web/TJPork.Web.csproj", "src/TJPork.Web/"]
RUN dotnet restore "src/TJPork.Web/TJPork.Web.csproj"

# Copy remaining source code
COPY . .
WORKDIR "/src/src/TJPork.Web"
RUN dotnet build "TJPork.Web.csproj" -c Release -o /app/build
RUN dotnet publish "TJPork.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TJPork.Web.dll"]
