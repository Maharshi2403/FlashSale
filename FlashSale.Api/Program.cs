using Inventory.InventorySchema;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InventorySchema>();




var app = builder.Build();


var inventory_schema = app.Services.GetRequiredService<InventorySchema>();


app.MapGet("/", () => "Route to /kinaxis.okta.com -> Autherize there -> validate here -> use this app");



app.Run();
