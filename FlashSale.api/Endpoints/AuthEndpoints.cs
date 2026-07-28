using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api.Endpoints;

public static class AuthEndpoints
{
    public record UserCredentials(string Username, string Password);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var route = app.MapGroup("/auth");

        route.MapPost("/signup", async (UserCredentials credentials, AppDbContext db) =>
        {
            if (string.IsNullOrEmpty(credentials.Username) ||
                string.IsNullOrEmpty(credentials.Password))
            {
                return Results.BadRequest("Username and password cannot be empty.");
            }

            if (!credentials.Username.EndsWith("@kinaxis.com") ||
                credentials.Password.Length < 6)
            {
                return Results.BadRequest("Username must end with '@kinaxis.com' and password must be at least 6 characters long.");
            }

            var userExists = await db.Users.AnyAsync(u => u.Email == credentials.Username);

            if (userExists)
            {
                return Results.BadRequest($"User '{credentials.Username}' already exists.");
            }

            User newUser = new()
            {
                Email = credentials.Username,
                PasswordHash = credentials.Password
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            return Results.Ok($"User '{credentials.Username}' signed up successfully.");
        });

        route.MapPost("/login", (UserCredentials user) =>
        {
            return Results.Ok("Login successful");
        });

        return app;
    }
}