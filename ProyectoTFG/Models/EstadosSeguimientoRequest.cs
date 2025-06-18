namespace ProyectoTFG.Models
{
    public class EstadosSeguimientoRequest
    {
        public int CurrentUserId { get; set; }
        public List<int> UserIds { get; set; }
    }

}
