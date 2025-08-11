using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using todolist.data;
using todolist.model;
using TaskStatus = todolist.model.TaskStatus;

namespace todolist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QuotaController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Gán quota cho user (dùng bởi admin)
        [HttpPost("assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignQuota([FromBody] WeeklyQuota quota)
        {
            // Check trùng quota cho cùng tuần/năm
            var exists = await _context.WeeklyQuotas.AnyAsync(q =>
                q.UserId == quota.UserId &&
                q.WeekNumber == quota.WeekNumber &&
                q.Year == quota.Year
            );

            if (exists)
                return BadRequest("Quota đã được gán cho user này trong tuần.");

            _context.WeeklyQuotas.Add(quota);
            await _context.SaveChangesAsync();
            return Ok(quota);
        }

        // ✅ Lấy quota tuần hiện tại của người dùng
        [HttpGet("my-week")]
        [Authorize]
        public async Task<IActionResult> GetMyQuotaStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var today = DateTime.UtcNow;
            var calendar = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            var weekNumber = calendar.GetWeekOfYear(today, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            var year = today.Year;

            var quota = await _context.WeeklyQuotas.FirstOrDefaultAsync(q =>
                q.UserId == userId &&
                q.WeekNumber == weekNumber &&
                q.Year == year);

            if (quota == null)
            {
                // Không có quota thì dùng mặc định
                quota = new WeeklyQuota
                {
                    UserId = userId,
                    TaskTarget = 5,
                    PointTarget = 50,
                    WeekNumber = weekNumber,
                    Year = year,
                };
            }

            // Lấy task trong tuần
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var tasks = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId &&
                            t.DueDate >= startOfWeek && t.DueDate < endOfWeek)
                .ToListAsync();

            var completedTasks = tasks.Count(t => t.Status == todolist.model.TaskStatus.Completed);
            var totalPoints = tasks.Where(t => t.Status == todolist.model.TaskStatus.Completed).Sum(t => t.Points);

            return Ok(new
            {
                quota.TaskTarget,
                quota.PointTarget,
                completedTasks,
                totalPoints,
                isTargetMet = completedTasks >= quota.TaskTarget && totalPoints >= quota.PointTarget
            });
        }

        // ✅ Xem toàn bộ quota (admin)
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllQuotas()
        {
            var quotas = await _context.WeeklyQuotas
                .Include(q => q.User)
                .ToListAsync();

            return Ok(quotas);
        }
        //Lấy tổng quan của user hiện tại trong 1 tuần (dashboard user)
        [HttpGet("weekly-summary")]
        [Authorize]
        public async Task<IActionResult> GetWeeklySummary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var today = DateTime.UtcNow;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var tasks = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId && t.DueDate >= startOfWeek && t.DueDate < endOfWeek)
                .ToListAsync();

            var completed = tasks.Count(t => t.Status == TaskStatus.Completed);
            var inProgress = tasks.Count(t => t.Status == TaskStatus.InProgress);
            var pending = tasks.Count(t => t.Status == TaskStatus.Pending);
            var points = tasks.Where(t => t.Status == TaskStatus.Completed).Sum(t => t.Points);

            return Ok(new
            {
                total = tasks.Count,
                completed,
                inProgress,
                pending,
                points
            });
        }

        //Xếp hạng người dùng theo điểm (cho admin)
        [HttpGet("user-leaderboard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserLeaderboard()
        {
            var today = DateTime.UtcNow;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var result = await _context.Tasks
                .Where(t => t.DueDate >= startOfWeek && t.DueDate < endOfWeek && t.Status == TaskStatus.Completed)
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new {
                    UserId = g.Key,
                    Points = g.Sum(t => t.Points),
                    TaskCount = g.Count()
                })
                .OrderByDescending(x => x.Points)
                .ToListAsync();

            return Ok(result);
        }
        //Thống kê trạng thái task toàn hệ thống (dùng cho biểu đồ Pie)
        [HttpGet("status-chart")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatusDistribution()
        {
            var data = await _context.Tasks
                .GroupBy(t => t.Status)
                .Select(g => new {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(data);
        }

    }
}
