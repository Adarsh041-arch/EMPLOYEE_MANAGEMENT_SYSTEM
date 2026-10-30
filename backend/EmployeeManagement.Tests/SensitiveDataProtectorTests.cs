using EmployeeManagement.Api.Services;

namespace EmployeeManagement.Tests;

public class SensitiveDataProtectorTests
{
    // 32 byte key, base64 - only for tests
    private const string Key = "dGVzdC1rZXktZm9yLXVuaXQtdGVzdHMtMzJieXRlcyE=";

    [Fact]
    public void Encrypt_then_decrypt_gives_back_the_value()
    {
        var protector = new SensitiveDataProtector(Key);
        var cipher = protector.Encrypt("123456789012");

        Assert.NotEqual("123456789012", cipher);
        Assert.DoesNotContain("123456789012", cipher);
        Assert.Equal("123456789012", protector.Decrypt(cipher));
    }

    [Fact]
    public void Same_value_encrypts_differently_each_time()
    {
        var protector = new SensitiveDataProtector(Key);
        Assert.NotEqual(protector.Encrypt("ABCDE1234F"), protector.Encrypt("ABCDE1234F"));
    }

    [Fact]
    public void Wrong_key_length_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => new SensitiveDataProtector(Convert.ToBase64String(new byte[10])));
    }

    [Theory]
    [InlineData("1234-5678-9012", "123456789012")]
    [InlineData("1234 5678 9012", "123456789012")]
    [InlineData("123456789012", "123456789012")]
    public void Aadhaar_is_normalised_to_digits(string input, string expected)
    {
        Assert.Equal(expected, SensitiveDataProtector.NormalizeAadhaar(input));
    }

    [Fact]
    public void Masks_hide_most_of_the_value()
    {
        Assert.Equal("XXXX-XXXX-9012", SensitiveDataProtector.MaskAadhaar("9012"));
        Assert.Equal("ABXXXXX34F", SensitiveDataProtector.MaskPan("ABCDE1234F"));
        Assert.Equal("1234-5678-9012", SensitiveDataProtector.FormatAadhaar("123456789012"));
    }
}
