using Inventory.InventorySchema;
using OrderEngine.DisruptorEngine;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InventorySchema>();

builder.Services.AddSingleton<DisruptorEngine>(on =>
{
    var Inventory = on.GetRequiredService<InventorySchema>();

    return new DisruptorEngine(
        Inventory,
        4096
    );
});


var app = builder.Build();

var disruptor = app.Services.GetRequiredService<DisruptorEngine>();
disruptor.Start();

app.MapGet("/", () => "Route to /kinaxis.okta.com -> Autherize there -> validate here -> use this app");



app.Run();
