using System.Diagnostics.CodeAnalysis;

namespace Shortr.Application;

// Base62 holds these characters: [0-9a-zA-Z]. The length of a code is 7.
// The number of possible codes is 62 to the power of 7. This number is
// approximately 3.5 trillion. Collisions are rare. The unique constraint
// rejects a duplicate code. The caller then generates a new code.
public sealed class ShortCodeGenerator
{
    public const int CodeLength = 7;

    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // This class stays an instance on purpose. Program.cs registers this
    // class in the DI container. LinkService unit tests fake this class.
    // Rule CA1822 requires static members when no instance data is in use.
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "DI registration and test fakes need an instance.")]
    public string Generate()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        }

        return new string(chars);
    }
}