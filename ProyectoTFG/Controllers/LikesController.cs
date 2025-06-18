using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoTFG.Models;

[ApiController]
[Route("api/likes")]
public class LikesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LikesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("post/{postId}")]
    public async Task<ActionResult> LikePost(int postId, int userId)
    {
        var like = new Like { PostId = postId, UserId = userId };
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("post/{postId}")]
    public async Task<ActionResult> UnlikePost(int postId, int userId)
    {
        var like = await _context.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        if (like == null)
        {
            return NotFound();
        }

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
