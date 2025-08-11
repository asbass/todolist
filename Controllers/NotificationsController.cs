using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using todolist.data;
using todolist.DTOs;
using todolist.model;
using Microsoft.EntityFrameworkCore;//cần để ý cái này làm gì
using TaskStatus = todolist.model.TaskStatus;

namespace todolist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var today = DateTime.UtcNow;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var notifications = new List<NotificationDto>();

            // 1. Gửi thông báo nếu có task sắp đến hạn (< 2 ngày)
            var soonDueTasks = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId &&
                            t.Status != TaskStatus.Completed &&
                            t.DueDate <= today.AddDays(2))
                .ToListAsync();

            foreach (var task in soonDueTasks)
            {
                notifications.Add(new NotificationDto
                {
                    Message = $"📌 Task \"{task.Title}\" sắp đến hạn vào {task.DueDate:dd/MM/yyyy}.",
                    Type = "warning",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 2. Gửi thông báo nếu chưa đạt quota giữa tuần trở đi (Thứ 4+)
            if ((int)today.DayOfWeek >= 3)
            {
                var quota = await _context.WeeklyQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId &&
                                              q.WeekStart == startOfWeek);

                if (quota != null)
                {
                    var weeklyTasks = await _context.Tasks
                        .Where(t => t.AssignedToUserId == userId &&
                                    t.DueDate >= startOfWeek && t.DueDate < endOfWeek)
                        .ToListAsync();

                    var completedTasks = weeklyTasks.Count(t => t.Status == TaskStatus.Completed);
                    var points = weeklyTasks.Where(t => t.Status == TaskStatus.Completed).Sum(t => t.Points);

                    if (completedTasks < quota.TaskTarget || points < quota.PointTarget)
                    {
                        notifications.Add(new NotificationDto
                        {
                            Message = $"⚠️ Bạn mới hoàn thành {completedTasks}/{quota.TaskTarget} task và {points}/{quota.PointTarget} điểm trong tuần này. Cố lên!",
                            Type = "info",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            return Ok(notifications);
        }
    }

}
