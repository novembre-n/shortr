using System.Diagnostics.CodeAnalysis;

namespace Shortr.Application;

// Base62: [0-9a-zA-Z]. 62^7 ≈ 3.5 trillion possible codes — collisions are rare;
// the repository's false return (unique constraint) handles the ones that happen anyway.
public sealed class ShortCodeGenerator
{
    public const int CodeLength = 7;

    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // Instance (not static) on purpose: this class gets registered in DI and
    // faked in LinkService unit tests. CA1822 would have it be static.
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