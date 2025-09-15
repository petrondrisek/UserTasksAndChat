namespace UserTasksAndChat.Controllers
{
    public class OperationResult<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public static OperationResult<T> Success(T data) => new()
        {
            IsSuccess = true,
            Data = data
        };

        public static OperationResult<T> Failure(string errorCode, string? errorMessage = null) => new()
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
