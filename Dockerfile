# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["SaigonStays.API/SaigonStays.API.csproj", "SaigonStays.API/"]
RUN dotnet restore "SaigonStays.API/SaigonStays.API.csproj"

# Copy full source and build
COPY . .
WORKDIR "/src/SaigonStays.API"
RUN dotnet build "SaigonStays.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SaigonStays.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Configure ASP.NET Core URL binding for Render (Default port 8080 or from PORT env)
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SaigonStays.API.dll"]
