using StackExchange.Redis;
using Valuator.Repository;
using Valuator.Repository;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        string redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "127.0.0.1:6379";
        
        builder.Services.AddSingleton<IConnectionMultiplexer>(options =>
            ConnectionMultiplexer.Connect(redisConnection));
        
        builder.Services.AddScoped<IRedisRepository, RedisRepository>();
        
        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
