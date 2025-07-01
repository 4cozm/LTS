using Microsoft.AspNetCore.SignalR;

namespace LTS.Services;

public class StatusHub : Hub
{
    public async Task NotifyConsent()
    {
        await Clients.All.SendAsync("ConsentReceived");
    }
}
