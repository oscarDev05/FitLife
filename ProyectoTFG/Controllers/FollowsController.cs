using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;
using System.Text.Json;

[ApiController]
[Route("api/follows")]
public class FollowsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FollowsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("followcount/{id}")]
    public async Task<ActionResult<object>> GetFollowCounts(int id)
    {
        var seguidores = await _context.Followers.CountAsync(f => f.FolloweeId == id);
        var seguidos = await _context.Followers.CountAsync(f => f.FollowerId == id);

        return Ok(new { seguidores, seguidos });
    }

    //[HttpGet("isFollowing")]
    //public async Task<ActionResult<bool>> CheckIfFollowing([FromQuery] int followerId, [FromQuery] int followingId)
    //{
    //    var exists = await _context.Followers.AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followingId);
    //    return Ok(exists);
    //}

    // Coger el estado de un user
    [HttpGet("getState")]
    public async Task<ActionResult<string>> GetRequestState([FromQuery] int senderId, [FromQuery] int receiverId)
    {
        var req = await _context.Solicitudes.AnyAsync(s => s.SenderId == senderId && s.ReceiverId == receiverId);
        var follow = await _context.Followers.AnyAsync(f => f.FollowerId == senderId && f.FolloweeId == receiverId);
        
        if (req)
        {
            return Ok("Pendiente");
        }
        else if(follow) 
        {
            return Ok("Siguiendo");
        }
        else
        {
            return Ok("Seguir");
        }
    }

    [HttpGet("followRequest")]
    public async Task<ActionResult<bool>> SendFollowRequest([FromQuery] int senderId, [FromQuery] int receiverId)
    {
        var exists = await _context.Solicitudes.AnyAsync(s => s.SenderId == senderId && s.ReceiverId == receiverId);

        if (exists)
            return BadRequest("Ya existe una solicitud pendiente entre estos usuarios.");

        var receiverUser = _context.Users.Find(receiverId);

        if (receiverUser.Privacy == true)   // Si el perfil es privado se manda la solicitud
        {
            var solicitud = new Solicitud
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                SentAt = DateTime.Now
            };

            _context.Solicitudes.Add(solicitud);
        }
        else   // si el perfil es público le sigue directamente.
        {
            var existingFollow = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == senderId && f.FolloweeId == receiverId);

            if (existingFollow == null)
            {
                var user = await _context.Users.FindAsync(senderId);
                var followee = await _context.Users.FindAsync(receiverId);

                if (user == null || followee == null)
                {
                    return NotFound("Usuario no encontrado.");
                }

                var follower = new Follower
                {
                    FollowerId = senderId,
                    FolloweeId = receiverId,
                    FollowerUser = user,
                    FolloweeUser = followee
                };
                _context.Followers.Add(follower);
            }
        }
        try
        {
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpGet("received")]
    public async Task<ActionResult<List<Solicitud>>> GetReceivedRequests([FromQuery] int userId)
    {
        var solicitudes = await _context.Solicitudes
            .Where(s => s.ReceiverId == userId)
            .Include(s => s.Sender)
            .ToListAsync();

        return Ok(solicitudes);
    }

    [HttpPost("responderSolicitud")]
    public async Task<IActionResult> ResponderSolicitud([FromBody] Dictionary<string, string> data)
    {
        if (data == null
            || !data.TryGetValue("solicitudId", out var solicitudIdStr)
            || !data.TryGetValue("accion", out var accion))
        {
            return BadRequest("Parámetros incorrectos.");
        }

        if (!int.TryParse(solicitudIdStr, out var solicitudId))
        {
            return BadRequest("SolicitudId inválido.");
        }

        var solicitud = await _context.Solicitudes.FindAsync(solicitudId);

        if (solicitud == null)
            return NotFound("Solicitud no encontrada.");

        if (accion != "Aceptada" && accion != "Rechazada")
            return BadRequest("Acción inválida.");

        if (accion == "Aceptada")
        {
            // Verificar si ya existe seguimiento
            var existingFollow = await _context.Followers
                .FirstOrDefaultAsync(f => f.FollowerId == solicitud.SenderId && f.FolloweeId == solicitud.ReceiverId);

            if (existingFollow == null)
            {
                var user = await _context.Users.FindAsync(solicitud.SenderId);
                var followee = await _context.Users.FindAsync(solicitud.ReceiverId);

                if (user == null || followee == null)
                {
                    return NotFound("Usuario no encontrado.");
                }

                var follower = new Follower
                {
                    FollowerId = solicitud.SenderId,
                    FolloweeId = solicitud.ReceiverId,
                    FollowerUser = user,
                    FolloweeUser = followee
                };
                _context.Followers.Add(follower);
            }
        }

        // Siempre eliminar la solicitud porque se aceptó o rechazó
        _context.Solicitudes.Remove(solicitud);

        try
        {
            await _context.SaveChangesAsync();
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpPost("estadosSeguimiento")]
    public async Task<ActionResult<Dictionary<int, string>>> GetEstadosSeguimiento([FromBody] EstadosSeguimientoRequest request)
    {
        if (request.UserIds == null || !request.UserIds.Any())
            return BadRequest("No se proporcionaron usuarios.");

        var estados = new Dictionary<int, string>();

        foreach (var id in request.UserIds)
        {
            bool isFollowing = await _context.Followers.AnyAsync(f =>
                f.FollowerId == request.CurrentUserId && f.FolloweeId == id);

            if (isFollowing)
            {
                estados[id] = "Siguiendo";
                continue;
            }

            bool pendiente = await _context.Solicitudes.AnyAsync(s =>
                s.SenderId == request.CurrentUserId && s.ReceiverId == id);

            if (pendiente)
            {
                estados[id] = "Pendiente";
                continue;
            }

            estados[id] = "Seguir";
        }

        return Ok(estados);
    }

    [HttpPost("toggleFollow")]
    public async Task<ActionResult<string>> ToggleFollow([FromBody] Dictionary<string, int> data)
    {
        if (data == null || !data.ContainsKey("UserId") || !data.ContainsKey("TargetId"))
            return BadRequest("Datos inválidos.");

        int userId = data["UserId"];
        int targetId = data["TargetId"];

        var existingFollow = await _context.Followers
            .FirstOrDefaultAsync(f => f.FollowerId == userId && f.FolloweeId == targetId);

        if (existingFollow != null)
        {
            _context.Followers.Remove(existingFollow);
            await _context.SaveChangesAsync();
            return Ok("Seguir");
        }

        var existingSolicitud = await _context.Solicitudes
            .FirstOrDefaultAsync(s => s.SenderId == userId && s.ReceiverId == targetId);

        if (existingSolicitud != null)
        {
            _context.Solicitudes.Remove(existingSolicitud);
            await _context.SaveChangesAsync();
            return Ok("Seguir");
        }

        var receiverUser = _context.Users.Find(targetId);
        if (receiverUser.Privacy == true)   // Si el perfil es privado se manda la solicitud
        {
            Console.WriteLine("trueeeee");
            var solicitud = new Solicitud
            {
                SenderId = userId,
                ReceiverId = targetId,
                SentAt = DateTime.Now
            };
            _context.Solicitudes.Add(solicitud);

            await _context.SaveChangesAsync();
            return Ok("Pendiente");
        }
        else
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null || receiverUser == null)
            {
                return NotFound("Usuario no encontrado.");
            }

            var follower = new Follower
            {
                FollowerId = userId,
                FolloweeId = targetId,
                FollowerUser = user,
                FolloweeUser = receiverUser
            };
            _context.Followers.Add(follower);
            await _context.SaveChangesAsync();
            return Ok("Siguiendo");
        }
    }
}

// Clase auxiliar para la petición estadosSeguimiento
public class EstadosSeguimientoRequest
{
    public int CurrentUserId { get; set; }
    public List<int> UserIds { get; set; }
}
