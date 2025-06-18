using ProyectoTFG.Models;

public class Solicitud
{
    public int Id { get; set; }
    public required int SenderId { get; set; }
    public required int ReceiverId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;
    public string Estado { get; set; } = "Pendiente";

    public User? Sender { get; set; }
    public User? Receiver { get; set; }
}
