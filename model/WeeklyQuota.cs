using System;

namespace todolist.model
{
    public class WeeklyQuota
    {
        public int Id { get; set; }

        // Ai được gán chỉ tiêu
        public string UserId { get; set; }
        public User User { get; set; }

        // Chỉ tiêu
        public int TaskTarget { get; set; } = 5;
        public int PointTarget { get; set; } = 50;

        // Xác định tuần
        public int WeekNumber { get; set; }
        public int Year { get; set; }

        // (Tuỳ chọn) Lưu lại tuần bắt đầu & kết thúc
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
    }
}
