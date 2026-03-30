FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Products/Products.csproj", "Products/"]
COPY ["src/DataEntities/DataEntities.csproj", "DataEntities/"]
COPY ["src/TinyShop.ServiceDefaults/TinyShop.ServiceDefaults.csproj", "TinyShop.ServiceDefaults/"]

RUN dotnet restore "Products/Products.csproj"

COPY src/ .
WORKDIR /src/Products
RUN dotnet publish "Products.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Products.dll"]