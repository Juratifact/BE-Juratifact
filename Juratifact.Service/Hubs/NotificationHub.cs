using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Juratifact.Service.Hubs;

[Authorize]
public class NotificationHub: Hub
{
    
}