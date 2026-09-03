
using FlashSale.Api.Endpoints;
using FlashSale.Api.Hubs;
using FlashSale.Api.OrderBook;
using FlashSale.Api.OrderBook.InventoryManager;

var builder = WebApplication.CreateBuilder(args);
var inventory_path= builder.Configuration["Inventory:FilePath"];
var orderbook_path= builder.Configuration["OrderBook:FilePath"];
// Let the hosting environment (ASPNETCORE_URLS / Render's $PORT) control the listening URL.
// Removed a hard-coded URL so the container/runtime can bind to the port Render provides.
//services

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
// builder.Services.AddSingleton<OrderChannel>();

builder.Services.AddSingleton<OrderQueue>();
// CORS: allow local front-end dev origins to call the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal", policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8443", "https://flash-sale-seven.vercel.app")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});


//inventory path never be null 
if (inventory_path == null)
{
    inventory_path = "data/itemlist.csv";
}

//  data.OrderId,
//              data.ProductId,
//              data.Quantity,
//              data.ReservationToken,
//              data.Price,
//              data.Timestamp

//add initial header line for csv orderbook
File.WriteAllText("data/orderlogs.csv", "OrderId, ProductId, Quantity, ReservationToken, Price, Timestamp\n" );

builder.Services.AddSingleton<DisruptorEngine>(_ =>
    new DisruptorEngine(
         inventory_path,
        _.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<InventoryHub>>(),
        4096));


var app = builder.Build();

var disruptorEngine = app.Services.GetRequiredService<DisruptorEngine>();
disruptorEngine.Start();

// For API visulization and testing

    app.UseSwagger();
    app.UseSwaggerUI();

// Enable CORS for local development UIs
app.UseCors("AllowLocal");
app.MapHub<InventoryHub>("/hubs/inventory");



app.MapSaleEndpoints();



app.Run();




