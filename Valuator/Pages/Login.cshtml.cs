using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Valuator.Models;
using Valuator.Repository;

namespace Valuator.Pages;

public class LoginPage: PageModel
{
    private readonly IRedisRepository _redis;

    public LoginPage(IRedisRepository redis)
    {
        _redis = redis;
    }

    [BindProperty]
    public string Login { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public async Task<IActionResult> OnPostAsync()
    {
        string redisKey = $"USER-{Login}";
        var json = _redis.Get("main", redisKey);

        if (string.IsNullOrEmpty(json))
        {
            return RedirectToPage("/Login");
        }

        var user = JsonSerializer.Deserialize<User>(json)!;
        var inputHash = HashPassword(Password);

        if (user.PasswordHash != inputHash)
        {
            return RedirectToPage("/Login");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        return RedirectToPage("/Index");
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}