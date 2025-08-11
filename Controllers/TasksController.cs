using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Text.Json;
using todolist.data;
using todolist.model;
using ThreadingTasks = System.Threading.Tasks;
namespace todolist.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasks()
        {
            return await _context.Tasks
                .Include(t => t.AssignedTo)
                .ToListAsync();
        }

        // GET: api/tasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            return task;
        }

        // POST: api/tasks
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TaskItem>> CreateTask([FromBody] TaskItem task)
        {
            // ✅ Lấy ID người đang đăng nhập từ JWT token
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Không xác định được người dùng.");

            // ✅ Gán người tạo task
            task.CreatedByUserId = userId;

            // (Tuỳ chọn) Kiểm tra xem AssignedToUserId có tồn tại trong hệ thống không
            if (!string.IsNullOrEmpty(task.AssignedToUserId))
            {
                var assignedUser = await _context.Users.FindAsync(task.AssignedToUserId);
                if (assignedUser == null)
                {
                    return BadRequest("Người được giao không tồn tại.");
                }

                task.AssignedTo = assignedUser; // (nếu bạn cần gán navigation)
            }

            // ✅ Thêm task vào DB
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateTask(int id, TaskItem updatedTask)
        {
            if (id != updatedTask.Id)
                return BadRequest();

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (task.CreatedByUserId != userId)
                return Forbid("Bạn không có quyền cập nhật task này.");

            // Cập nhật thủ công từng thuộc tính để tránh ghi đè CreatedBy
            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.DueDate = updatedTask.DueDate;
            task.Points = updatedTask.Points;
            task.AssignedToUserId = updatedTask.AssignedToUserId;
            ///    // ✅ Ghi log vào bảng AuditLogs

            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Update",
                EntityType = "Task",
                EntityId = task.Id,
                Timestamp = DateTime.UtcNow,
                Data = System.Text.Json.JsonSerializer.Serialize(task)
            });
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (task.CreatedByUserId != userId)
                return Forbid("Bạn không có quyền xoá task này.");

            _context.Tasks.Remove(task);
            // Log delete
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "Delete",
                EntityType = "Task",
                EntityId = task.Id,
                Timestamp = DateTime.UtcNow,
                Data = JsonSerializer.Serialize(task)
            });
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //Lọc task theo người tạo hoặc người được giao
        // GET: api/tasks/created-by-me
        [HttpGet("created-by-me")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasksCreatedByMe()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return await _context.Tasks
                .Where(t => t.CreatedByUserId == userId)
                .Include(t => t.AssignedTo)
                .ToListAsync();
        }

        // GET: api/tasks/assigned-to-me
        [HttpGet("assigned-to-me")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetTasksAssignedToMe()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId)
                .Include(t => t.AssignedTo)
                .ToListAsync();
        }
        // API đổi trạng thái task
        // PUT: api/tasks/{id}/status
        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] todolist.model.TaskStatus newStatus)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return NotFound();

            // Chỉ người được giao mới được đổi trạng thái
            if (task.AssignedToUserId != userId)
                return Forbid("Bạn không được phép thay đổi trạng thái task này.");

            task.Status = newStatus;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //Thống kê số lượng task và điểm của người dùng
        // GET: api/tasks/stats/my
        [HttpGet("stats/my")]
        [Authorize]
        public async Task<IActionResult> GetMyTaskStats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var total = await _context.Tasks
                .CountAsync(t => t.AssignedToUserId == userId);

            var completed = await _context.Tasks
                .CountAsync(t => t.AssignedToUserId == userId && t.Status == todolist.model.TaskStatus.Completed);

            var points = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId && t.Status == todolist.model.TaskStatus.Completed)
                .SumAsync(t => t.Points);

            return Ok(new
            {
                TotalTasks = total,
                CompletedTasks = completed,
                TotalPoints = points
            });
        }
        ///Thống kê Task theo tuần
        [HttpGet("stats/week")]
        [Authorize]
        public async Task<IActionResult> GetWeeklyStats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var today = DateTime.UtcNow;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            // Task giao cho user trong tuần
            var weeklyTasks = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId && t.DueDate >= startOfWeek && t.DueDate < endOfWeek)
                .ToListAsync();

            var total = weeklyTasks.Count;
            var completed = weeklyTasks.Count(t => t.Status == todolist.model.TaskStatus.Completed);
            var totalPoints = weeklyTasks
                .Where(t => t.Status == todolist.model.TaskStatus.Completed)
                .Sum(t => t.Points);

            return Ok(new
            {
                Week = $"{startOfWeek:yyyy-MM-dd} → {endOfWeek.AddDays(-1):yyyy-MM-dd}",
                TotalTasks = total,
                CompletedTasks = completed,
                TotalPoints = totalPoints
            });
        }
        //Thống kê tất cả user (admin)
        [HttpGet("stats/week/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetWeeklyStatsAllUsers()
        {
            var today = DateTime.UtcNow;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday).Date;
            var endOfWeek = startOfWeek.AddDays(7);

            var stats = await _context.Users.Select(user => new
            {
                user.Id,
                user.UserName,
                TotalTasks = _context.Tasks.Count(t =>
                    t.AssignedToUserId == user.Id &&
                    t.DueDate >= startOfWeek && t.DueDate < endOfWeek),

                CompletedTasks = _context.Tasks.Count(t =>
                    t.AssignedToUserId == user.Id &&
                    t.DueDate >= startOfWeek && t.DueDate < endOfWeek &&
                    t.Status == todolist.model.TaskStatus.Completed),

                TotalPoints = _context.Tasks
                    .Where(t => t.AssignedToUserId == user.Id &&
                                t.DueDate >= startOfWeek && t.DueDate < endOfWeek &&
                                t.Status == todolist.model.TaskStatus.Completed)
                    .Sum(t => (int?)t.Points) ?? 0
            }).ToListAsync();

            return Ok(stats);
        }
        //API thống kê trạng thái task toàn hệ thống
        [HttpGet("status-chart")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatusChart()
        {
            var chart = await _context.Tasks
                .GroupBy(t => t.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(chart);
        }

    }
}
