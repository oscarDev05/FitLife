using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ChatController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-conversation")]
    public async Task<IActionResult> CreateConversation(int userId1, int userId2)
    {
        var u1 = Math.Min(userId1, userId2);
        var u2 = Math.Max(userId1, userId2);

        var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Id == u1);
        var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Id == u2);

        if (user1 == null || user2 == null)
            return BadRequest("Uno de los usuarios no existe.");

        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                (c.UserId1 == u1 && c.UserId2 == u2) ||
                (c.UserId1 == u2 && c.UserId2 == u1));


        if (existing != null)
            return Ok(existing);

        var conversation = new Conversation
        {
            UserId1 = u1,
            UserId2 = u2,
            User1 = user1,
            User2 = user2
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        Console.WriteLine("-----------CONVERSACIÓN CREADA-----------");

        return Ok(conversation);
    }




    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromBody] Message message)
    {
        if (!_context.Users.Any(u => u.Id == message.SenderId) ||
            !_context.Users.Any(u => u.Id == message.ReceiverId) ||
            !_context.Conversations.Any(c => c.Id == message.ConversationId))
        {
            return BadRequest("Datos inválidos.");
        }

        message.SentAt = DateTime.UtcNow;
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        Console.WriteLine("-----------MENSAJE ENVIADO-----------");

        return Ok(new
        {
            id = message.Id,
            content = message.Content,
            senderId = message.SenderId,
            receiverId = message.ReceiverId,
            conversationId = message.ConversationId,
            sentAt = message.SentAt
        });
    }



    [HttpGet("get-messages/{conversationId}")]
    public async Task<IActionResult> GetMessages(int conversationId)
    {
        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        Console.WriteLine("----------> CONVERSACION ID: " + conversationId);
        Console.WriteLine("----------> MENSAJES CARGADOS: " + messages.Count);

        return Ok(messages);
    }


    [HttpPost("mark-messages-read")]
    public async Task<IActionResult> MarkMessagesAsRead(int conversationId, int userId)
    {
        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in messages)
        {
            msg.IsRead = true;
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("searchFollowee")]
    public async Task<ActionResult<User[]>> GetFolloweeByName([FromQuery] string word, [FromQuery] int currentUserId)
    {
        // Verificar si el usuario es Pro
        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null)
        {
            return NotFound("Usuario no encontrado.");
        }

        if (currentUser.IsPro == true)
        {
            // Si es Pro: Buscar todos los usuarios que coincidan con el nombre (excepto él mismo)
            var allMatchingUsers = await _context.Users
                .Where(u => u.Id != currentUserId &&
                            u.UserName.ToLower().Contains(word.ToLower()))
                .Take(20)
                .ToListAsync();

            // Obtener los IDs de los usuarios que ya sigue
            var followedIds = await _context.Followers
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            // Marcar cuáles están seguidos
            foreach (var user in allMatchingUsers)
            {
                user.IsFollowed = followedIds.Contains(user.Id);
            }

            return allMatchingUsers.ToArray();
        }
        else
        {
            // Si NO es Pro: comportamiento actual
            var usuariosSeguidos = await _context.Followers
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FolloweeUser)
                .ToListAsync();

            var resultado = usuariosSeguidos
                .Where(u => u.UserName.Contains(word, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            foreach (var u in resultado)
            {
                u.IsFollowed = true;
            }

            return resultado.ToArray();
        }
    }



    [HttpGet("conversations/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetUsersWithConversations(int userId)
    {
        var conversations = await _context.Conversations
            .AsNoTracking()
            .Where(c => c.UserId1 == userId || c.UserId2 == userId)
            .ToListAsync();

        if (!conversations.Any())
            return Ok(new List<object>());

        var conversationIds = conversations.Select(c => c.Id).ToList();

        var lastMessages = await _context.Messages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => new {
                ConversationId = g.Key,
                LastMessageDate = g.Max(m => m.SentAt)
            })
            .ToListAsync();

        var conversationsWithLastMessage = conversations
            .Join(lastMessages,
                c => c.Id,
                lm => lm.ConversationId,
                (c, lm) => new {
                    Conversation = c,
                    LastMessageDate = lm.LastMessageDate
                })
            .OrderByDescending(x => x.LastMessageDate)
            .ToList();

        var userIdsOrdered = conversationsWithLastMessage
            .Select(cwlm => cwlm.Conversation.UserId1 == userId ? cwlm.Conversation.UserId2 : cwlm.Conversation.UserId1)
            .Distinct()
            .ToList();

        var usersDict = await _context.Users
            .AsNoTracking()
            .Where(u => userIdsOrdered.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var unreadCounts = await _context.Messages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId))
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, UnreadCount = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.UnreadCount);

        var result = new List<object>();

        foreach (var cwlm in conversationsWithLastMessage)
        {
            var conversation = cwlm.Conversation;
            var otherUserId = conversation.UserId1 == userId ? conversation.UserId2 : conversation.UserId1;

            if (usersDict.TryGetValue(otherUserId, out var user))
            {
                unreadCounts.TryGetValue(conversation.Id, out int unreadCount);

                result.Add(new
                {
                    user,
                    unreadMessagesCount = unreadCount
                });
            }
        }

        // Filtrar para eliminar usuarios repetidos en 'result'
        var distinctResult = result
            .GroupBy(r => ((dynamic)r).user.Id) // Agrupas por user.Id
            .Select(g => g.First())              // Solo tomas el primero de cada grupo
            .ToList();

        Console.WriteLine("----------> CONVERSACIONES CARGADAS: " + distinctResult.Count);

        return Ok(distinctResult);
    }

    [HttpGet("getUnreadCount")]
    public async Task<int> GetUnreadCountAsync(int conversationId, int userId)
    {
        return await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.ReceiverId == userId && !m.IsRead)
            .CountAsync();
    }

}