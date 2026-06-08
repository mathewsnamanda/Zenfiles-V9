using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace testing_scheduler_with_signalir.notitication
{
    public class NotificationHub : Hub
    {
        // Join a group with a user-specific identifier
        public async Task JoinUserGroup(string groupName, string userId)
        {
            // Add connection to group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"{groupName}/{userId}");

            // Optionally, set the user identifier (if you want to use Clients.User)
            Context.Items["UserId"] = userId;
        }
    }

}
