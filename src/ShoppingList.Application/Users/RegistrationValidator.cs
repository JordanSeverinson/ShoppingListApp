using System.Text.RegularExpressions;

namespace ShoppingList.Application.Users;

public static partial class RegistrationValidator
{
    [GeneratedRegex(@"^[a-zA-Z0-9 ]+$")]
    private static partial Regex PreferredNamePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public static string? ValidatePreferredName(string? preferredName)
    {
        var trimmed = preferredName?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return "Preferred name must be at least 2 characters.";
        }

        if (!PreferredNamePattern().IsMatch(trimmed))
        {
            return "Preferred name cannot contain special characters.";
        }

        return null;
    }

    public static string? ValidateEmail(string? email)
    {
        var trimmed = email?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return "Email is required.";
        }

        if (!EmailPattern().IsMatch(trimmed))
        {
            return "Enter a valid email address.";
        }

        return null;
    }

    public static string? ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = PhoneNormalizer.Normalize(phone);
        if (digits.Length != 10)
        {
            return "Phone number must be a 10-digit number.";
        }

        return null;
    }

    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 6)
        {
            return "Password must be at least 6 characters.";
        }

        return null;
    }

    public static string FormatPhone(string phone)
    {
        var digits = PhoneNormalizer.Normalize(phone);
        if (digits.Length != 10)
        {
            return phone.Trim();
        }

        return $"{digits[..3]}-{digits[3..6]}-{digits[6..]}";
    }
}
