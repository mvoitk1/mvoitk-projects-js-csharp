using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using com.akaver.Domain.Base;

namespace App.Domain.ApiKey.SimpleList
{
    public class ListItem: DomainEntityId
    {
        [MinLength(1)] [MaxLength(255)] 
        public string Description { get; set; } = default!;

        public bool Completed { get; set; }

        [JsonIgnore]
        public Guid ApiKeyId { get; set; }
        [JsonIgnore]
        public ApiKey? ApiKey { get; set; }
    }
}