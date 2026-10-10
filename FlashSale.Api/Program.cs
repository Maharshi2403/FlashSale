using Inventory.InventorySchema;
using Endpoints;
using OrderChanel.ServiceChannel;
using OrderEngine.DisruptorEngine;
using Service.ServicesHandler;
using Services.NotificationHandler;




var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<InventorySchema>();

builder.Services.AddSingleton<ServiceHandler, OrderWriter>();
builder.Services.AddHttpClient<NotificationHandler>(client =>
{
    client.BaseAddress = new Uri("https://api.brevo.com/v3/");
});
builder.Services.AddSingleton<ServiceHandler>(services =>
    services.GetRequiredService<NotificationHandler>());


builder.Services.AddSingleton<ServiceChannel>();

builder.Services.AddSingleton<DisruptorEngine>(on =>
{
    var Inventory = on.GetRequiredService<InventorySchema>();
    var channel = on.GetRequiredService<ServiceChannel>();
    
    return new DisruptorEngine(
        Inventory,
        channel,
        4096
    );
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapUserEndpoints();
app.MapOrderEndpoints();

var disruptor = app.Services.GetRequiredService<DisruptorEngine>();
disruptor.Start();

app.MapGet("/", () => "Route to /kinaxis.okta.com -> Autherize there -> validate here -> use this app");



app.Run();
