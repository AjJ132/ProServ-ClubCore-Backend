using Microsoft.AspNetCore.SignalR;


public class MyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Handle the event when a new connection is established
        await Clients.Caller.SendAsync("Connected", "You are connected to the hub for ProServ University.");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // Handle the event when a connection is terminated
        await base.OnDisconnectedAsync(exception);
    }

    public async Task TerminateConnection()
    {
        // Custom method to terminate a connection
        await Clients.Caller.SendAsync("Disconnected", "You have been disconnected from the ProServ University hub.");
        Context.Abort();
    }
}
