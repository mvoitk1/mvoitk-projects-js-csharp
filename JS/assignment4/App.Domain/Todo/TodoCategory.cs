using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using App.Domain.Identity;
using com.akaver.Contracts.Domain.Base;
using com.akaver.Domain.Base;

namespace App.Domain.Todo
{
    public class TodoCategory: DomainEntityId, IDomainAppUser<AppUser>
    {
        public Guid AppUserId { get; set; }
        [JsonIgnore]
        public AppUser? AppUser { get; set; }
        
        [MaxLength(128)]
        public string CategoryName { get; set; } = default!;

        public int CategorySort { get; set; }

        public DateTime SyncDt { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? Tag { get; set; }
        
        [JsonIgnore]
        public ICollection<TodoTask>? TodoTasks { get; set; }

    }
}