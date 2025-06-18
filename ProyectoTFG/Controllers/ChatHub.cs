using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task SendMessage(string senderId, string receiverId, string message, int conversationId)
    {
        var senderIdInt = int.Parse(senderId);
        var receiverIdInt = int.Parse(receiverId);

        var msgToSend = new
        {
            Content = message,
            SenderId = senderIdInt,
            ReceiverId = receiverIdInt,
            ConversationId = conversationId,
            SentAt = DateTime.UtcNow
        };

        // Enviar mensaje a grupos de sender y receiver
        await Clients.Group(receiverId).SendAsync("ReceiveMessage", msgToSend);
        await Clients.Group(senderId).SendAsync("ReceiveMessage", msgToSend);

        // Calcular número de mensajes no leídos para el receptor
        var unreadCount = await GetUnreadCountAsync(conversationId, receiverIdInt);

        // Enviar evento para actualizar contador exacto solo al receptor
        await Clients.Group(receiverId).SendAsync("UpdateUnreadCount", new
        {
            ConversationId = conversationId,
            UnreadCount = unreadCount
        });
    }


    public async Task<int> GetUnreadCountAsync(int conversationId, int userId)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.ReceiverId == userId && !m.IsRead)
            .CountAsync();
    }


    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.Request.Query["userId"];
        var conversationId = httpContext?.Request.Query["conversationId"];

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        if (!string.IsNullOrEmpty(conversationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        await base.OnConnectedAsync();
    }



    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"];
        if (!string.IsNullOrEmpty(userId))
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }
        return base.OnDisconnectedAsync(exception);
    }


    public async Task Typing(string senderId, string conversationId)
    {
        await Clients.Group(conversationId).SendAsync("UserTyping", senderId);
    }
}
