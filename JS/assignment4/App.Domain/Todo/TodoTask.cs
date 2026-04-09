using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using com.akaver.Domain.Base;

namespace App.Domain.Todo
{
    public class TodoTask: DomainEntityId
    {
        [MaxLength(128)]
        public string TaskName { get; set; } = default!;


        public int TaskSort { get; set; }

        public DateTime CreatedDt { get; set; } = DateTime.UtcNow;
        
        public DateTime? DueDt { get; set; }
        
        public bool IsCompleted { get; set; }

        public bool IsArchived { get; set; }
        
        public Guid TodoCategoryId { get; set; }
        [JsonIgnore]
        public TodoCategory? TodoCategory { get; set; }

        public Guid TodoPriorityId { get; set; }
        [JsonIgnore]
        public TodoPriority? TodoPriority { get; set; }
        
        public DateTime SyncDt { get; set; } = DateTime.UtcNow;

    }
}