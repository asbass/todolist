using System;
namespace todolist.model
{
    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public int Points { get; set; }
        public DateTime DueDate { get; set; }

        public string AssignedToUserId { get; set; }
        public User AssignedTo { get; set; }
        public string CreatedByUserId { get; set; }
        public User CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }

}
