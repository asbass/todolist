using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using todolist.data;
using TaskStatus = todolist.model.TaskStatus;
using Microsoft.EntityFrameworkCore;
namespace todolist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Xếp hạng người dùng theo tổng điểm tuần
        [HttpGet("user-leaderboard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserLeaderboard()
        {
            var today = DateTime.UtcNow;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var ranking = await _context.Tasks
                .Where(t => t.DueDate >= startOfWeek && t.DueDate < endOfWeek && t.Status == TaskStatus.Completed)
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalPoints = g.Sum(t => t.Points),
                    CompletedTasks = g.Count()
                })
                .OrderByDescending(x => x.TotalPoints)
                .ToListAsync();

            return Ok(ranking);
        }
    }
}
