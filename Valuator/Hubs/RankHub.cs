using Microsoft.AspNetCore.SignalR;

namespace Valuator.Hubs;

public class RankHub : Hub
{
    // Браузер вызывает этот метод, чтобы подписаться на получение ранга конкретного текста
    public async Task JoinTextGroup(string textId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, textId);
    }
}
