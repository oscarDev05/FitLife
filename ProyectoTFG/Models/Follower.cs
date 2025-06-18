namespace ProyectoTFG.Models
{
    public class Follower
    {
        public int Id { get; set; }

        // Relaciones
        public required int FollowerId { get; set; }
        public required User FollowerUser { get; set; }

        public required int FolloweeId { get; set; }
        public required User FolloweeUser { get; set; }
    }
}
