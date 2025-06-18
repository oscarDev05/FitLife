using Microsoft.VisualBasic;

namespace ProyectoTFG.Models
{
    public class Message
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; } = false;

        // Relaciones
        public required int SenderId { get; set; }
        public User? Sender { get; set; }

        public required int ReceiverId { get; set; }
        public User? Receiver { get; set; }

        public required int ConversationId { get; set; }
        public Conversation? Conversation { get; set; }
    }
}
