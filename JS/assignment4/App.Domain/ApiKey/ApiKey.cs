using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using App.Domain.Identity;
using com.akaver.Domain.Base;

namespace App.Domain.ApiKey
{
    public class ApiKey: DomainEntityId
    {
        public Guid SecretKey { get; set; } = Guid.NewGuid();
        
        public bool IsDisabled { get; set; }

        [MinLength(1)] [MaxLength(255)] public string AppName { get; set; } = default!;
      
        [JsonIgnore]
        public Guid AppUserId { get; set; } = default!;
        [JsonIgnore]
        public AppUser? AppUser { get; set; }

    }
}