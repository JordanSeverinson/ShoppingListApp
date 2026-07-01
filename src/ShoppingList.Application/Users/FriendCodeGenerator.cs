using System.Security.Cryptography;

namespace ShoppingList.Application.Users;

public static class FriendCodeGenerator
{
    private const int CodeLength = 8;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        Span<byte> randomBytes = stackalloc byte[CodeLength];

        RandomNumberGenerator.Fill(randomBytes);
        for (var i = 0; i < CodeLength; i++)
        {
            buffer[i] = Alphabet[randomBytes[i] % Alphabet.Length];
        }

        return new string(buffer);
    }
}
