using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Dto.Mission
{
    public class CreateMissionDto
    {
        [Required]
        [MinLength(5)]
        [MaxLength(100)]
        public required string Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [DataType(DataType.DateTime)]
        [FutureDate]
        public DateTime? Deadline { get; set; } = null;
        public Guid? UserId { get; set; } = null;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string[] Files { get; set; } = Array.Empty<string>();
        public Guid[] RelatedUserIds { get; set; } = Array.Empty<Guid>();
    }
}
