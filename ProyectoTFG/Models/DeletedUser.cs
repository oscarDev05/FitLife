using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoTFG.Models
{
    public class DeletedUser
    {
        public int Id { get; set; }  // Será el mismo que el Id del User eliminado

        public string UserName { get; set; } = "";  // Se usa el 'Usuario eliminado' pero prefiero mantenerlo.

        public string Email { get; set; } = "";

        public DateTime DeletedAt { get; set; } = DateTime.Now;
    }
}
