using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace todolist.model
{
    public class User : IdentityUser
    {
        public ICollection<TaskItem> Tasks { get; set; }
    }
}
