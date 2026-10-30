using System.Security.Cryptography;
using System.Text;

namespace EmployeeManagement.Api.Services;

// Encrypts Aadhaar / PAN with AES-GCM before they go into the database.
// The key comes from configuration (Encryption:Key, 32 bytes base64).
public class SensitiveDataProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public SensitiveDataProtector(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (base64 encoded).");
    }

    public string Encrypt(string plainText)
    {
        var plain = Encoding.UTF8.GetBytes(plainText);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        // stored as nonce + tag + cipher in one base64 string
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Decrypt(string encrypted)
    {
        var data = Convert.FromBase64String(encrypted);
        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var cipher = data.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public static string NormalizeAadhaar(string aadhaar) =>
        new string((aadhaar ?? string.Empty).Where(c => c != '-' && c != ' ').ToArray());

    public static string FormatAadhaar(string digits) =>
        $"{digits[..4]}-{digits[4..8]}-{digits[8..]}";

    public static string MaskAadhaar(string last4) => $"XXXX-XXXX-{last4}";

    public static string MaskPan(string pan) => pan[..2] + "XXXXX" + pan[7..];
}
