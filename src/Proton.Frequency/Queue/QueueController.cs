using MQTTnet.Server;
using Proton.Frequency.Services;

namespace Proton.Frequency.Queue; 

internal class QueueController {
    public static QueueController CreateInstance() {
        return new QueueController();
    }
    internal static Task OnClientConnected(ClientConnectedEventArgs eventArgs)
    {
        var logger = Initializer.GetLogger<WebApplication>();
        logger.LogInformation("Client '{id}' connected.", eventArgs.ClientId);
        return Task.CompletedTask;
    }

    internal static Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
    {
        var logger = Initializer.GetLogger<WebApplication>();
        logger.LogInformation("Client '{id}' ants to connect. Accepting!", eventArgs.ClientId);
        return Task.CompletedTask;
    }
}