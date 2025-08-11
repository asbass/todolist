namespace todolist.model
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; } // Create / Update / Delete
        public string EntityType { get; set; } // "Task"
        public int EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Data { get; set; } // JSON snapshot
    }

}
