using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace NotificationHubLambda
{
    class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            //TODO Write to DB a list of connections
            return base.OnConnectedAsync();
        }

    }
}
