using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valuator.Models;
using Valuator.Repository;

namespace Valuator.Pages;

public class Register: PageModel
{
    private readonly IRedisRepository _redis;

    public Register(IRedisRepository redis)
    {
        _redis = redis;
    }

    [BindProperty]
    public string Login { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public IActionResult OnPost()
    {
        string redisKey = $"USER-{Login}";
        var existing = _redis.Get("main", redisKey);

        if (!string.IsNullOrEmpty(existing))
        {
            return RedirectToPage("/Register");
        }

        var user = new User
        {
            Login = Login,
            PasswordHash = HashPassword(Password)
        };
        
        var json = JsonSerializer.Serialize(user);
        _redis.Set("main", redisKey, json);

        return RedirectToPage("/Login");
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}