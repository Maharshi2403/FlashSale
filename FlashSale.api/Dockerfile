FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY . .

RUN dotnet restore FlashSale.api.csproj

RUN dotnet publish FlashSale.api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app

COPY --from=build /app/publish .

# Include inventory CSV in the runtime image so the app can find /app/OrderBook/itemlist.csv
COPY --from=build /src/OrderBook/itemlist.csv /app/OrderBook/itemlist.csv

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "FlashSale.api.dll"]