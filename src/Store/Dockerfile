FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/Store/Store.csproj", "Store/"]
COPY ["src/DataEntities/DataEntities.csproj", "DataEntities/"]
COPY ["src/TinyShop.ServiceDefaults/TinyShop.ServiceDefaults.csproj", "TinyShop.ServiceDefaults/"]

RUN dotnet restore "Store/Store.csproj"

COPY src/ .
WORKDIR /src/Store
RUN dotnet publish "Store.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Store.dll"]
