using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.Mission
{
    public class UpdateMissionDto
    {
        [Required]
        public required Guid Id { get; set; }

        [MinLength(5)]
        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [DataType(DataType.DateTime)]
        [FutureDate]
        public DateTime? Deadline { get; set; } = null;
        public Guid? UserId { get; set; } = null;
        public string[]? Tags { get; set; } = null;
        public string[]? StoredFiles { get; set; } = null;
        public Guid[]? RelatedUserIds { get; set; } = null;
        public bool? IsCompleted { get; set; } = null;
    }
}
