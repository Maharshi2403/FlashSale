//Model for User

public class User
{
    public int Id { get; set; }
    
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }

    public bool isAdmin { get; set; } = false;

    public bool userVerified { get; set; } = false;
    
}

