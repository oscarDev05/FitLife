using ProyectoTFG.Models;

public class Conversation
{
    public int Id { get; set; }
    public int UserId1 { get; set; }
    public int UserId2 { get; set; }

    // Relaciones
    public required User User1 { get; set; }
    public required User User2 { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
