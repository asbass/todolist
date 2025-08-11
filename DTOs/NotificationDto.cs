namespace todolist.DTOs
{
    public class NotificationDto
    {
        public string Message { get; set; }
        public string Type { get; set; } // info, warning, success, error
        public DateTime CreatedAt { get; set; }
    }
}
