var builder = WebApplication.CreateBuilder(args);


//services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();


if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

app.UseHttpsRedirection();


// [/status] designed to return current session's stats
app.MapGet("/status", () =>
{
    var status = "API is alive and running.";
    return Results.Ok(status);
})
.WithName("GetStatus");


//signup
app.MapPost("/signup", (UserCredentials credentials) => {
    var response = $"User '{credentials.Username}' signed up successfully.";
    return Results.Ok(response);
   
});

//login
app.MapPost("/login", (UserCredentials credentials) => {
    var response = $"User '{credentials.Username}' logged in successfully.";
    return Results.Ok(response);
});


app.Run();


record UserCredentials(string Username, string Password);

