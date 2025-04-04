using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using Valuator.Repository;
using Valuator.Publisher;
using Valuator.Repository;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using StackExchange.Redis;
    
namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        string redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
        
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
        
        builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
        builder.Services.AddScoped<IRedisRepository, RedisRepository>();
        
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>(); 
        
        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redisConnection, "DataProtection-Keys")
            .SetApplicationName("Valuator");
        
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
