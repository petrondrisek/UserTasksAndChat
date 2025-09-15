using System.ComponentModel.DataAnnotations;

namespace UserTasksAndChat.Extensions
{
    public class ValidGuidAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null) 
                return false;
            return Guid.TryParse(value.ToString(), out _);
        }
    }
}