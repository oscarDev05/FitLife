namespace ProyectoTFG.Models
{
    public class Like
    {
        public int Id { get; set; }

        // Relaciones
        public required int PostId { get; set; }
        public Post? Post { get; set; }

        // Usuario que da el like.
        public required int? UserId { get; set; }
        public User? User { get; set; }
    }
}
