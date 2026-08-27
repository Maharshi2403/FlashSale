using Microsoft.EntityFrameworkCore;
using FlashSale.Api.Endpoints;
using FlashSale.Api.Hubs;
using FlashSale.Api.OrderBook;
using FlashSale.Api.OrderBook.InventoryManager;

var builder = WebApplication.CreateBuilder(args);

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
        .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://localhost:8443")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
//populate inventory

builder.Services.AddSingleton<DisruptorEngine>(_ =>
    new DisruptorEngine(
        builder.Environment.ContentRootPath,
        _.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<InventoryHub>>(),
        4096));



//db connection
// Only add DbContext if a DefaultConnection is provided. This allows running without Postgres.
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
bool useDatabase = !string.IsNullOrEmpty(defaultConn);
if (useDatabase)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(defaultConn));
}



var app = builder.Build();

var disruptorEngine = app.Services.GetRequiredService<DisruptorEngine>();
disruptorEngine.Start();

// For API visulization and testing

    app.UseSwagger();
    app.UseSwaggerUI();

// Enable CORS for local development UIs
app.UseCors("AllowLocal");
app.MapHub<InventoryHub>("/hubs/inventory");

// Map endpoints. Auth endpoints require a configured database; skip them if no DB is provided.
if (useDatabase)
{
    app.MapAuthEndpoints();
}
else
{
    // Optionally expose a minimal /auth route indicating auth is disabled in this build.
    app.MapGet("/auth", () => Results.Ok("Auth endpoints are disabled (no DB configured)."));
}

app.MapSaleEndpoints();
// [/status] designed to return current session's stats
// app.MapGet("/status", () =>
// {
//     var status = "API is alive and running.";
//     return Results.Ok(status);
// })
// .WithName("GetStatus");



// //signup
// app.MapPost("/signup", async(UserCredentials credentials, AppDbContext db) => {

//      //validate entry
//     if(string.IsNullOrEmpty(credentials.Username) || string.IsNullOrEmpty(credentials.Password)){
    
//         return Results.BadRequest("Username and password cannot be empty.");
//     } 

//     // validatge kinaxis email
//     if(!credentials.Username.EndsWith("@kinaxis.com") || credentials.Password.Length < 6){
//         return Results.BadRequest("Username must end with '@kinaxis.com' and password must be at least 6 characters long.");
//     }
//     // if user already exists
//     var userExists = await db.Users.AnyAsync(u => u.Email == credentials.Username);
//     if(userExists){
//         var userfound = $"User '{credentials.Username}' already exists.";
//         return Results.BadRequest(userfound);
//     }

    

//      User newUser = new User{
//         Email = credentials.Username,
//         PasswordHash = credentials.Password
//     };
//     db.Users.Add(newUser);
//     await db.SaveChangesAsync();
//     var response = $"User '{credentials.Username}' signed up successfully.";
//     return Results.Ok(response);
   
// });

// //login
// app.MapPost("/login", (UserCredentials credentials) => {
//     var response = $"User '{credentials.Username}' logged in successfully.";
//     return Results.Ok(response);
// });


app.Run();


record UserCredentials(string Username, string Password);

