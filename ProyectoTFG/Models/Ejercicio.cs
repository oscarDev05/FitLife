namespace ProyectoTFG.Models
{
    public class Ejercicio
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Sets { get; set; }
        public string Repetitions { get; set; }
        public string? File {  get; set; }

        // Relación con User
        public int UserId { get; set; }
        public User? User { get; set; }

        public Ejercicio() { }

        public Ejercicio(int userId, string name, int sets, string repetitions)
        {
            UserId = userId;
            Name = name;
            Sets = sets;
            Repetitions = repetitions;
        }
    }
}
