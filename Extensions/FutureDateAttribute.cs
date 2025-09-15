using System;
using System.ComponentModel.DataAnnotations;

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value != null && value is DateTime dateTime)
        {
            return dateTime > DateTime.Now;
        }

        return true;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be a date in future.";
    }
}