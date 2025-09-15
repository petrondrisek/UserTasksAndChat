namespace UserTasksAndChat.Dto
{
    public class RequestDto
    {
        public int Offset { get; set; } = 0;
        public int Limit { get; set; } = 10;
        public string? Search { get; set; } = null;
    }
}
