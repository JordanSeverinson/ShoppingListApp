namespace ShoppingList.Application.Users;

public static class PhoneNormalizer
{
    public static string Normalize(string phone) =>
        new(phone.Where(char.IsDigit).ToArray());
}
