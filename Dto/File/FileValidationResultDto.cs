namespace UserTasksAndChat.Dto.File
{
    public record FileValidationResultDto(bool IsValid, string? ErrorMessage = null)
    {
        public static FileValidationResultDto Valid() => new(true);
        public static FileValidationResultDto Invalid(string errorMessage) => new(false, errorMessage);
    }
}
