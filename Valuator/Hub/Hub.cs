using Microsoft.AspNetCore.SignalR;

namespace Valuator.Hub;

public class ResultHub : Microsoft.AspNetCore.SignalR.Hub
{
    
    public async Task SendAsync(string method, string id)
    {
        await Clients.All.SendAsync(method, id);
    }
}