

public static class AuthEndpoints
{
    



    public  static IEndpointRouteBuilder MapAuthEndpoint(IEndpointRouteBuilder app)
    {
        
        var route = app.MapGroup("/Auth");

         //Autherization 
         
         route.MapGet("/", (HttpContent httpcontext) =>
         {
            
         });











        return app;

    }
}