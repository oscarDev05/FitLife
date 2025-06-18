using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("check")]
    public async Task<ActionResult<User>> GetUserByData(string userName, string passwd)
    {
        var users = await _context.Users
            .Where(u => u.UserName == userName)
            .ToListAsync();

        var user = users.FirstOrDefault(u => u.UserName == userName && u.Password == passwd);

        if (user == null)
        {
            return Unauthorized("Usuario o contraseña incorrectos.");
        }

        return Ok(user);
    }

    [HttpPost("sports={id}")]
    public async Task<ActionResult<User[]>> GetUserBySport(int id)
    {
        var recomendados = new List<User>();

        // Obtener IDs de usuarios ya seguidos por el usuario logueado
        var siguiendoIds = await _context.Followers
            .Where(f => f.FollowerId == id)
            .Select(f => f.FolloweeId)
            .ToListAsync();

        // Obtener todos los usuarios menos el logueado y los que ya sigue
        var allUsers = await _context.Users
            .Include(u => u.Followers)
            .Where(u => u.Id != id && !siguiendoIds.Contains(u.Id))
            .ToListAsync();

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var deportes = user.Lista_deportes?.Split(',') ?? [];

        // Clasificamos los usuarios en los 4 grupos según prioridad
        var grupo1 = new List<User>(); // Pro + deporte común
        var grupo2 = new List<User>(); // No pro + deporte común
        var grupo3 = new List<User>(); // Pro + sin deporte común
        var grupo4 = new List<User>(); // No pro + sin deporte común

        foreach (var u in allUsers)
        {
            bool tieneDeporteComun = false;

            if (!string.IsNullOrEmpty(u.Lista_deportes))
            {
                var lista = u.Lista_deportes.Split(',');
                tieneDeporteComun = lista.Any(d => deportes.Contains(d));
            }

            if (tieneDeporteComun && u.IsPro.GetValueOrDefault())
                grupo1.Add(u);
            else if (tieneDeporteComun && !u.IsPro.GetValueOrDefault())
                grupo2.Add(u);
            else if (!tieneDeporteComun && u.IsPro.GetValueOrDefault())
                grupo3.Add(u);
            else
                grupo4.Add(u);
        }

        // Agregar en orden hasta completar 5 usuarios
        recomendados.AddRange(grupo1.Take(5 - recomendados.Count));
        if (recomendados.Count < 5) recomendados.AddRange(grupo2.Take(5 - recomendados.Count));
        if (recomendados.Count < 5) recomendados.AddRange(grupo3.Take(5 - recomendados.Count));
        if (recomendados.Count < 5) recomendados.AddRange(grupo4.Take(5 - recomendados.Count));

        // Ya no necesitas marcar .IsFollowed porque todos no están seguidos
        return recomendados.ToArray();
    }


    [HttpGet("searchUser")]
    public async Task<ActionResult<User[]>> GetUserByName([FromQuery] string word, [FromQuery] int currentUserId)
    {
        var allUsers = await _context.Users
            .Where(u => u.Id != currentUserId) // no incluir al usuario loggeado
            .ToListAsync();

        var coinciden = allUsers
            .Where(u => u.UserName.Contains(word, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        //Obtener id de usuarios seguidos
        var siguiendoIds = await _context.Followers
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FolloweeId)
            .ToListAsync();

        foreach (var u in coinciden)
        {
            u.IsFollowed = siguiendoIds.Contains(u.Id);
        }

        return coinciden.ToArray();
    }


    [HttpGet("searchPost")]
    public async Task<ActionResult<List<Post>>> GetPostByDesc([FromQuery] string word, [FromQuery] int currentUserId)
    {
        // Paso 1: cargar posts permitidos
        var posts = await _context.Posts
            .Include(p => p.User)
            .Where(p => p.UserId != currentUserId && p.User.Privacy == false)
            .ToListAsync();

        // Paso 2: filtrar en memoria por contenido
        var filtrados = posts
            .Where(p => !string.IsNullOrEmpty(p.Content) &&
                        p.Content.Contains(word, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();

        return filtrados;
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        return user;
    }

    ////////////////////////////////////////////////////////////////////
    [HttpPut("UpdateUser")]
    public async Task<ActionResult<User>> UpdateUser([FromBody] User newUser)
    {
        if (newUser == null)
        {
            return BadRequest("Datos inválidos");
        }

        var user = await _context.Users.FindAsync(newUser.Id);
        if (user == null)
        {
            return NotFound("Usuario no encontrado");
        }

        // Guardar imagen en el servidor (si hay una nueva imagen en base64)
        if(newUser.Foto_perfil == "RESET_TO_DEFAULT")
        {
            user.Foto_perfil = $"wwwroot/users_images/foto_perfil_default.png";
        }
        else if (!string.IsNullOrEmpty(newUser.Foto_perfil) && newUser.Foto_perfil.StartsWith("data:image") && !newUser.Foto_perfil.Contains("foto_perfil_default.png"))
        {
            try
            {
                var base64Data = newUser.Foto_perfil.Split(',')[1];
                var imageBytes = Convert.FromBase64String(base64Data);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "users_images");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var fileName = $"{user.Id}.jpg";
                var filePath = Path.Combine(folderPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // Ruta relativa para guardar en la BBDD
                user.Foto_perfil = $"wwwroot/users_images/{fileName}";
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar la imagen: {ex.Message}");
            }
        }

        user.UserName = newUser.UserName;
        user.Email = newUser.Email;
        user.Password = newUser.Password;
        user.Description = newUser.Description;
        user.Privacy = newUser.Privacy;
        user.IsPro = newUser.IsPro;
        user.Lista_deportes = newUser.Lista_deportes;

        try
        {
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al actualizar usuario: {ex.Message}");
        }
    }


    ////////////////////////////////////////////////////////////////////

    [HttpGet("checkRegister")]
    public async Task<ActionResult<bool>> CheckExistUser(string userName, string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName || u.Email == email);

        if (user == null)
        {
            return false;
        }

        return true;
    }

    [HttpGet("checkUpdate")]
    public async Task<ActionResult<object>> CheckExistUserForUpdate(int id, string userName, string email)
    {
        var userNameExists = _context.Users.Any(u => u.UserName == userName && u.Id != id);
        var emailExists = _context.Users.Any(u => u.Email == email && u.Id != id);

        if (!userNameExists && !emailExists)
        {
            return Ok(new { exists = false });
        }

        return Ok(new
        {
            exists = userNameExists || emailExists,
            userNameExists = userNameExists,
            emailExists = emailExists
        });
    }


    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        if (user == null)
            return BadRequest("El objeto usuario es nulo.");

        var userExists = await _context.Users.AnyAsync(u => u.UserName == user.UserName || u.Email == user.Email);
        if (userExists)
            return BadRequest("El nombre de usuario o correo electrónico ya están en uso.");

        user.CreatedAt = DateTime.UtcNow;

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.Users
                .Include(u => u.Posts)
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .Include(u => u.Conversations)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound("Usuario no encontrado.");

            // 1. Guardar datos en DeletedUser
            // Que permita especificar el id.
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT DeletedUsers ON");

            var deletedUser = new DeletedUser
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                DeletedAt = DateTime.UtcNow
            };
            _context.DeletedUsers.Add(deletedUser);
            await _context.SaveChangesAsync();

            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT DeletedUsers OFF");

            // 2. Eliminar followers donde sea seguidor o seguido
            var followers = _context.Followers
                .Where(f => f.FollowerId == user.Id || f.FolloweeId == user.Id);
            _context.Followers.RemoveRange(followers);

            // 3. Eliminar mensajes enviados y recibidos
            var messages = _context.Messages
                .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id);
            _context.Messages.RemoveRange(messages);

            // 4. Eliminar conversaciones en las que participa
            var userConversations = _context.Conversations
                .Where(c => c.UserId1 == user.Id || c.UserId2 == user.Id);
            _context.Conversations.RemoveRange(userConversations);

            var solicitudes = _context.Solicitudes
               .Where(s => s.SenderId == user.Id || s.ReceiverId == user.Id);

            _context.Solicitudes.RemoveRange(solicitudes);

            // 6. Eliminar posts y sus archivos multimedia
            var userPosts = await _context.Posts
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            foreach (var post in userPosts)
            {
                if (!string.IsNullOrEmpty(post.File))
                {
                    var filePath = Path.Combine("wwwroot/post_files", post.File);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }

            _context.Posts.RemoveRange(userPosts);

            // 7. Finalmente, eliminar el usuario
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return Ok("Usuario eliminado correctamente.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            var inner = ex.InnerException?.Message ?? "";
            return StatusCode(500, $"Error al eliminar el usuario: {ex.Message} | {inner}");
        }
    }

    [HttpPost("addEjercicio")]
    public async Task<ActionResult<Ejercicio>> AddEjercicio(
    [FromForm] int userId,
    [FromForm] string name,
    [FromForm] int sets,
    [FromForm] string repetitions,
    [FromForm] IFormFile? file)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        // Primero, crear el ejercicio sin el archivo
        var ejercicio = new Ejercicio(userId, name, sets, repetitions);
        _context.Ejercicios.Add(ejercicio);
        await _context.SaveChangesAsync(); // Aquí se genera el ID

        if (file != null && file.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ejercicio_files");
            Directory.CreateDirectory(uploadsFolder);

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{ejercicio.Id}{extension}"; // Ejemplo: 12.jpg o 5.mp4
            var relativePath = Path.Combine("ejercicio_files", fileName);
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Actualiza la ruta del archivo en el ejercicio
            ejercicio.File = relativePath;
            await _context.SaveChangesAsync();
        }

        return Ok(ejercicio);
    }



    [HttpGet("getEjercicios")]
    public async Task<ActionResult<List<Ejercicio>>> GetEjerciciosByUser(int userId)
    {
        var ejercicios = await _context.Ejercicios.Where(e => e.UserId == userId).ToListAsync();
        if (ejercicios == null || ejercicios.Count == 0)
        {
            return new List<Ejercicio>();
        }

        return ejercicios;
    }

    [HttpPost("modEjercicio")]
    public async Task<ActionResult<Ejercicio>> ModEjercicio(int id, string name, int sets, string repetitions)
    {
        var ej = await _context.Ejercicios.FirstOrDefaultAsync(e => e.Id == id);
        if (ej == null)
        {
            return NotFound();
        }

        ej.Name = name;
        ej.Sets = sets;
        ej.Repetitions = repetitions;

        await _context.SaveChangesAsync();
        return Ok(ej); // Devuelve el ejercicio actualizado
    }


    [HttpDelete("delEjercicio")]
    public async Task<ActionResult<bool>> DelEjercicio(int id)
    {
        var ej = await _context.Ejercicios.FirstOrDefaultAsync(e => e.Id == id);
        if (ej == null)
        {
            return NotFound();
        }

        _context.Ejercicios.Remove(ej);
        await _context.SaveChangesAsync();
        return Ok(true);
    }

}
