using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Hubs;

namespace TmsApi.Application.Hubs;
public class TmsHub : Hub<ITmsHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var studentId = Context.GetHttpContext()?.Request.Query["studentId"].ToString();
        if (!string.IsNullOrWhiteSpace(studentId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Student(studentId));
        }
        await base.OnConnectedAsync();
    }
    public async Task JoinCourseGroup(string courseCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Course(courseCode));

    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
