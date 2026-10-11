using Shortr.Application;

namespace Shortr.UnitTests;

public sealed class ShortCodeGeneratorTests
{
    private const string Base62Alphabet =
        "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [Fact]
    public void Generate_ReturnsCodeOfConfiguredLength()
    {
        var generator = new ShortCodeGenerator();

        var code = generator.Generate();

        Assert.Equal(ShortCodeGenerator.CodeLength, code.Length);
    }

    [Fact]
    public void Generate_UsesOnlyBase62Characters()
    {
        var generator = new ShortCodeGenerator();

        for (var i = 0; i < 100; i++)
        {
            var code = generator.Generate();
            Assert.All(code, c => Assert.Contains(c, Base62Alphabet));
        }
    }

    [Fact]
    public void Generate_DoesNotRepeatInSmallSample()
    {
        var generator = new ShortCodeGenerator();
        var codes = Enumerable.Range(0, 1000).Select(_ => generator.Generate()).ToHashSet();

        Assert.Equal(1000, codes.Count);
    }
}