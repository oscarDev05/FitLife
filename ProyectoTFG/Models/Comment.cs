namespace ProyectoTFG.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public required int PostId { get; set; }
        public required int? UserId { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Post? Post { get; set; }
        public User? User { get; set; }
    }

}
