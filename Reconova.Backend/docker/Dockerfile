FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Reconova.Domain/Reconova.Domain.csproj", "Reconova.Domain/"]
COPY ["src/Reconova.Application/Reconova.Application.csproj", "Reconova.Application/"]
COPY ["src/Reconova.Infrastructure/Reconova.Infrastructure.csproj", "Reconova.Infrastructure/"]
COPY ["src/Reconova.Api/Reconova.Api.csproj", "Reconova.Api/"]

RUN dotnet restore "Reconova.Api/Reconova.Api.csproj"

COPY src/ .

WORKDIR "/src/Reconova.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reconova.Api.dll"]
