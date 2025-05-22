namespace Valuator.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}