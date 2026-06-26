namespace ShoppingList.Application.Lists;

public static class ShareCodeGenerator
{
    private const int CodeLength = 11;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            buffer[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(buffer);
    }
}
