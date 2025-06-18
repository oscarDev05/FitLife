using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;

namespace ProyectoTFG.Controllers
{
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PostsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Post>>> GetPosts()
        {
            try
            {
                var posts = await _context.Posts.ToListAsync();
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Post>> GetPost(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Likes)  // Asegúrate de incluir los likes
                .FirstOrDefaultAsync(p => p.Id == id);
            if (post == null)
            {
                return NotFound("Post no encontrado.");
            }

            return post;
        }

        ///////////////////////////////////////////////////////////////////
        [HttpPost]
        public async Task<ActionResult<Post>> AddPost([FromBody] JsonElement postData)
        {
            try
            {
                if (!postData.TryGetProperty("content", out JsonElement contentElement) ||
                    !postData.TryGetProperty("userId", out JsonElement userIdElement))
                {
                    return BadRequest("Faltan campos obligatorios.");
                }

                string content = contentElement.GetString();
                if (content == null) return BadRequest("El campo 'content' no puede ser nulo.");

                int userId = userIdElement.GetInt32();
                string mediaType = "none";
                string filePathInDb = null;
                byte[] fileBytes = null;
                string extension = null;

                if (postData.TryGetProperty("file", out JsonElement imageElement))
                {
                    string base64Data = imageElement.GetString();
                    var base64Parts = base64Data.Split(',');
                    if (base64Parts.Length == 2)
                    {
                        fileBytes = Convert.FromBase64String(base64Parts[1]);
                        mediaType = base64Data.Contains("video") ? "video" : "image";
                        extension = mediaType == "video" ? ".mp4" : ".jpg";
                    }
                    else return BadRequest("Formato base64 inválido.");
                }

                bool isAnuncio = false;
                if (postData.TryGetProperty("isAnuncio", out JsonElement isAnuncioElement))
                {
                    isAnuncio = isAnuncioElement.GetBoolean();
                }

                string deporteRelacionado = null;
                if (postData.TryGetProperty("deporteRelacionado", out JsonElement deporteRelacionadoElement) && deporteRelacionadoElement.ValueKind == JsonValueKind.String)
                {
                    deporteRelacionado = deporteRelacionadoElement.GetString();
                }


                var post = new Post
                {
                    Content = content,
                    File = null,
                    MediaType = mediaType,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsAnuncio = isAnuncio,
                    DeporteRelacionado = deporteRelacionado
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync(); // genera post.Id

                if (fileBytes != null)
                {
                    var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var videoFolder = Path.Combine(wwwrootPath, "post_files");
                    var thumbFolder = Path.Combine(wwwrootPath, "miniaturas");

                    Directory.CreateDirectory(videoFolder);
                    Directory.CreateDirectory(thumbFolder);

                    var fileName = $"{post.Id}{extension}";
                    var fullVideoPath = Path.Combine(videoFolder, fileName);
                    await System.IO.File.WriteAllBytesAsync(fullVideoPath, fileBytes);

                    post.File = $"/post_files/{fileName}";

                    // Si es video, generar miniatura
                    if (mediaType == "video")
                    {
                        var thumbnailPath = Path.Combine(thumbFolder, $"{post.Id}.jpg");

                        // FFmpeg: extraer miniatura al segundo 1
                        var ffmpegArgs = $"-i \"{fullVideoPath}\" -ss 00:00:01.000 -vframes 1 \"{thumbnailPath}\"";

                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                                Arguments = ffmpegArgs,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        string output = await process.StandardError.ReadToEndAsync();
                        process.WaitForExit();

                        if (!System.IO.File.Exists(thumbnailPath))
                        {
                            Console.WriteLine("FFmpeg error:\n" + output);
                            return StatusCode(500, "Error al generar la miniatura del video.");
                        }

                        post.Thumbnail = $"/miniaturas/{post.Id}.jpg";
                    }


                    _context.Posts.Update(post);
                    await _context.SaveChangesAsync();
                }

                return CreatedAtAction(nameof(GetPost), new { id = post.Id }, post);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Error interno: " + ex.Message);
            }
        }

        ///////////////////////////////////////////////////////////////////

        [HttpGet("user/{userId}")]
        public IActionResult GetPostsByUser(int userId)
        {
            var posts = _context.Posts
                .Where(p => p.UserId == userId)
                .Include(p => p.Comments)
                .Include(p => p.User)
                .Select(p => new {
                    p.Id,
                    p.Content,
                    p.Likes,
                    p.File,
                    p.UserId,
                    p.MediaType,
                    p.Thumbnail,
                    CommentsCount = _context.Comments.Count(c => c.PostId == p.Id)
                })
                .ToList();
            return Ok(posts);
        }

        [HttpGet("followedPosts")]
        public async Task<ActionResult> GetFollowedPosts(int userId)
        {
            // IDs usuarios que sigue
            var followedUserIds = await _context.Followers
                .Where(f => f.FollowerId == userId)
                .Select(f => f.FolloweeId)
                .ToListAsync();

            // IDs posts que le gustan al usuario
            var likedPostIds = await _context.Likes
                .Where(l => l.UserId == userId)
                .Select(l => l.PostId)
                .ToListAsync();

            // Deporte de interés del usuario
            var user = await _context.Users.FindAsync(userId);
            var listaDeportes = user?.Lista_deportes?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim().ToLower())
                .ToList() ?? new List<string>();

            // --- Anuncios relevantes ---
            var anunciosQuery = _context.Posts
                .Where(p =>
                    p.IsAnuncio &&
                    p.UserId != userId &&
                    p.DeporteRelacionado != null &&
                    listaDeportes.Contains(p.DeporteRelacionado.ToLower())
                )
                .Include(p => p.User);

            // Traer todos los anuncios y ordenarlos con seguido primero, luego no seguido, luego por fecha
            var anunciosList = await anunciosQuery
                .OrderByDescending(p => followedUserIds.Contains(p.UserId)) // true primero (seguidos)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Mapear anuncios a la forma deseada
            var anunciosDto = anunciosList.Select(p => new
            {
                p.Id,
                p.Content,
                p.File,
                p.CreatedAt,
                p.MediaType,
                p.Thumbnail,
                p.UserId,
                Likes = _context.Likes.Count(l => l.PostId == p.Id),
                isLiked = likedPostIds.Contains(p.Id),
                CommentsCount = _context.Comments.Count(c => c.PostId == p.Id),
                p.IsAnuncio,
                p.DeporteRelacionado
            }).ToList();

            // --- Otros posts normales de usuarios seguidos ---
            var otrosPosts = await _context.Posts
                .Where(p =>
                    !p.IsAnuncio &&
                    p.UserId != userId &&
                    followedUserIds.Contains(p.UserId)
                )
                .Include(p => p.Comments)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.File,
                    p.CreatedAt,
                    p.MediaType,
                    p.Thumbnail,
                    p.UserId,
                    Likes = _context.Likes.Count(l => l.PostId == p.Id),
                    isLiked = likedPostIds.Contains(p.Id),
                    CommentsCount = _context.Comments.Count(c => c.PostId == p.Id),
                    p.IsAnuncio,
                    p.DeporteRelacionado
                })
                .ToListAsync();

            // --- Concatenar:
            // primero anuncios (ya ordenados con seguidos antes)
            // luego otros posts seguidos

            var allPosts = anunciosDto
                .Concat(otrosPosts)
                .Take(10);

            return Ok(allPosts);
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound("Post no encontrado.");
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
