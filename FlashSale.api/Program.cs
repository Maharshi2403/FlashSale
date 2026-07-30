using Microsoft.EntityFrameworkCore;
using FlashSale.Api.Endpoints;
using FlashSale.Api.OrderBook;
using FlashSale.Api.OrderBook.Inventory;
using FlashSale.Api.OrderBook.OrderChannel;

var builder = WebApplication.CreateBuilder(args);


//services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<OrderQueue>();
//populate inventory
Inventory inventory = new Inventory();
inventory.PopulateInventory();
builder.Services.AddSingleton<Inventory>(inventory);
builder.Services.AddSingleton<OrderChannel>();

//db connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));



var app = builder.Build();

// For API visulization and testing
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

app.UseHttpsRedirection();

app.MapAuthEndpoints();
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

