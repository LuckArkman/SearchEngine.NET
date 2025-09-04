FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SearchEngine.ApiGateway/SearchEngine.ApiGateway.csproj", "SearchEngine.ApiGateway/"]
COPY ["SearchEngine.UserAuthService/SearchEngine.UserAuthService.csproj", "SearchEngine.UserAuthService/"]
COPY ["SearchEngine.Shared.Contracts/SearchEngine.Shared.Contracts.csproj", "SearchEngine.Shared.Contracts/"]
RUN dotnet restore "SearchEngine.ApiGateway/SearchEngine.ApiGateway.csproj"
COPY . .
WORKDIR "/src/SearchEngine.ApiGateway"
RUN dotnet build "./SearchEngine.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SearchEngine.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SearchEngine.ApiGateway.dll"]
