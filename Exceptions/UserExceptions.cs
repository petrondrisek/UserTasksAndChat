namespace UserTasksAndChat.Exceptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string message) : base(message) { }
    }

    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException(string message) : base(message) { }
    }

}
