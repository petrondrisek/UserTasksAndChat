namespace UserTasksAndChat.Dto.File
{
    public record FileInfoDto
    {
        public string Name { get; init; } = string.Empty;
        public long Size { get; init; }
        public string SizeFormatted { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; }
        public DateTime ModifiedDate { get; init; }
        public string Extension { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
    }
}
