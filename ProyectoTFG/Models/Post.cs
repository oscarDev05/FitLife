using System.Text.Json.Serialization;

namespace ProyectoTFG.Models
{
    public class Post
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public string? File { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? MediaType { get; set; }
        public bool isLiked { get; set; } = false;
        public required int UserId { get; set; }
        public User? User { get; set; }
        public string? Thumbnail { get; set; } // Ruta a la miniatura
        public bool IsAnuncio { get; set; } = false;
        public string? DeporteRelacionado { get; set; }


        public ICollection<Comment>? Comments { get; set; }
        public ICollection<Like>? Likes { get; set; }
    }
}
