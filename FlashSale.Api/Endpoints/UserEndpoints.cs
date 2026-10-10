using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Endpoints;

public sealed record UserResponse(int Id, string Name, string Email);

public sealed record CreateUserRequest(string Name, string Email);

public sealed record UpdateUserRequest(string Name, string Email);

public static class UserEndpoints
{
    private static readonly ConcurrentDictionary<int, UserResponse> Users = new();
    private static int _nextId;

    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users");

        users.MapGet("/", () => TypedResults.Ok(Users.Values.OrderBy(user => user.Id)))
            .WithSummary("List users");

        users.MapGet("/{id:int}",
                Results<Ok<UserResponse>, NotFound> (int id) =>
                    Users.TryGetValue(id, out var user)
                        ? TypedResults.Ok(user)
                        : TypedResults.NotFound())
            .WithSummary("Get a user by ID");

        users.MapPost("/",
                (CreateUserRequest request) =>
                {
                    var id = Interlocked.Increment(ref _nextId);
                    var user = new UserResponse(id, request.Name, request.Email);
                    Users[id] = user;
                    return TypedResults.Created($"/api/users/{id}", user);
                })
            .WithSummary("Create a user");

        users.MapPut("/{id:int}",
                Results<Ok<UserResponse>, NotFound> (int id, UpdateUserRequest request) =>
                {
                    if (!Users.ContainsKey(id))
                    {
                        return TypedResults.NotFound();
                    }

                    var user = new UserResponse(id, request.Name, request.Email);
                    Users[id] = user;
                    return TypedResults.Ok(user);
                })
            .WithSummary("Update a user");

        users.MapDelete("/{id:int}",
                Results<NoContent, NotFound> (int id) =>
                    Users.TryRemove(id, out _)
                        ? TypedResults.NoContent()
                        : TypedResults.NotFound())
            .WithSummary("Delete a user");
    }
}