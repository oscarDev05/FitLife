using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;

[ApiController]
[Route("api/posts/{postId}/comments")]
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CommentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    //[HttpGet]
    //public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsByPost(int postId)
    //{
    //    var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
    //    if (!postExists)
    //    {
    //        return NotFound("Post no encontrado.");
    //    }

    //    var comments = await _context.Comments
    //        .Where(c => c.PostId == postId)
    //        .Include(c => c.User)
    //        .OrderByDescending(c => c.CreatedAt)
    //        .ToListAsync();

    //    // SIEMPRE devuelve 200, aunque no haya comentarios
    //    return Ok(comments);
    //}

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetCommentsByPost(int postId)
    {
        var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
        if (!postExists)
        {
            return NotFound("Post no encontrado.");
        }

        var comments = await _context.Comments
            .Where(c => c.PostId == postId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        // Selecciona solo lo necesario, y sustituye username si User == null
        var result = comments.Select(c => new
        {
            c.Id,
            c.PostId,
            c.UserId,
            Username = c.User != null ? c.User.UserName : "Usuario eliminado",
            c.Content,
            c.CreatedAt
        });

        return Ok(comments);
    }




    [HttpPost]
    public async Task<ActionResult<Comment>> AddComment(int postId, [FromBody] Comment comment)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
        {
            return NotFound("Post no encontrado.");
        }

        comment.PostId = postId;
        comment.User = await _context.Users.FindAsync(comment.UserId);
        comment.CreatedAt = DateTime.Now;

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return Ok(comment);
    }


    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(int postId, int commentId)
    {
        var comment = await _context.Comments
            .Where(c => c.PostId == postId && c.Id == commentId)
            .FirstOrDefaultAsync();

        if (comment == null)
        {
            return NotFound("Comentario no encontrado.");
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

}
