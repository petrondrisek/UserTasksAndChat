namespace UserTasksAndChat.Dto
{
    public class GetResponseDto<T>
    {
        public int TotalCount { get; set; }
        public T[] Items { get; set; } = Array.Empty<T>();
    }
}
