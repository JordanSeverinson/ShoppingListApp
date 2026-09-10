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

        if (password.Length < 12)
        {
            return "Password must be at least 12 characters.";
        }

        if (password.Length > 128)
        {
            return "Password must be at most 128 characters.";
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);

        if (!hasUpper || !hasLower || !hasDigit)
        {
            return "Password must include an uppercase letter, a lowercase letter, and a number.";
        }

        return null;
    }

    public static readonly HashSet<string> AllowedGenders =
        new(StringComparer.OrdinalIgnoreCase) { "Male", "Female", "Non-binary" };

    public static string? ValidateGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return null;
        }

        return AllowedGenders.Contains(gender.Trim())
            ? null
            : "Select a valid gender option.";
    }

    public static string CanonicalGender(string gender) =>
        AllowedGenders.First(value =>
            string.Equals(value, gender.Trim(), StringComparison.OrdinalIgnoreCase));

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
