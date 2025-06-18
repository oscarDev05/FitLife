using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProyectoTFG.Models
{
    public class User
    {
        public int Id { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? Description { get; set; }
        public string? Foto_perfil { get; set; }

        public bool? Privacy { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? Lista_deportes { get; set; } = "";
        public bool? IsPro { get; set; } = false;

        [NotMapped]
        public bool IsFollowed { get; set; } = false;   // campo temporal para coprobar si el usuario lo sigue.



        // Relaciones
        public ICollection<Post>? Posts { get; set; }
        public ICollection<Comment>? Comments { get; set; }
        public ICollection<Like>? Likes { get; set; }
        public ICollection<Follower>? Followers { get; set; }
        public ICollection<Follower>? Following { get; set; }
        public ICollection<Ejercicio>? Ejercicios { get; set; }
        public ICollection<Message>? SentMessages { get; set; }
        public ICollection<Message>? ReceivedMessages { get; set; }
        public ICollection<Conversation>? Conversations { get; set; }
    }
}
