using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class EncryptionService
{
    private static RSA rsa;
    private static string privateKeyPem = "";

    // ✅ Load RSA Private Key (From AWS or Local)
    public static void LoadPrivateKey(string pemKey)
    {
        // ✅ Extract private key from AWS Secrets JSON format
        var privateKeyObject = JsonSerializer.Deserialize<Dictionary<string, string>>(pemKey);
        if (!privateKeyObject.ContainsKey("PKCS"))
        {
            throw new Exception("Invalid format: Private key not found in AWS Secrets Manager.");
        }
        pemKey = privateKeyObject["PKCS"];

        if (string.IsNullOrWhiteSpace(pemKey))
        {
            throw new Exception("Private key is empty or null.");
        }

        privateKeyPem = pemKey.Trim();

        // ✅ Remove PEM headers and format for Base64 decoding
        string base64Key = privateKeyPem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Trim();

        try
        {
            byte[] privateKeyBytes = Convert.FromBase64String(base64Key);
            rsa = RSA.Create();

            // ✅ Explicitly check and use correct import method
            if (privateKeyPem.Contains("RSA PRIVATE KEY"))
            {
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            }
            else
            {
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            }
        }
        catch (FormatException ex)
        {
            throw new Exception("Invalid Base64 encoding in private key. Check AWS Secrets format.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new Exception("Failed to import RSA private key. Ensure correct PKCS#8 or PKCS#1 format.", ex);
        }
    }

    // ✅ Generate and Return Public Key in PEM Format
    public static string GetPublicKeyPem()
    {
        if (rsa == null)
            throw new Exception("Private key not loaded!");

        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var base64PublicKey = Convert.ToBase64String(publicKeyBytes);

        var pemFormattedKey = new StringBuilder();
        pemFormattedKey.AppendLine("-----BEGIN PUBLIC KEY-----");
        for (int i = 0; i < base64PublicKey.Length; i += 64)
        {
            pemFormattedKey.AppendLine(base64PublicKey.Substring(i, Math.Min(64, base64PublicKey.Length - i)));
        }
        pemFormattedKey.AppendLine("-----END PUBLIC KEY-----");
        Console.WriteLine("Public Key:\n" + pemFormattedKey.ToString());

        return pemFormattedKey.ToString();
    }

    // ✅ Encrypt Data using Public Key (Accepts PEM format)
    public static string Encrypt(string plaintext, string publicKeyPem)
    {
        try
        {
            using var rsaEncryptor = RSA.Create();

            // ✅ Ensure the Public Key is in Correct PEM Format
            if (!publicKeyPem.Contains("-----BEGIN PUBLIC KEY-----"))
            {
                throw new Exception("Invalid public key format. Expected PEM format.");
            }

            // ✅ Extract Base64 content of Public Key
            string base64Key = publicKeyPem
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "")
                .Trim();

            // ✅ Convert Base64 Key to Bytes
            byte[] publicKeyBytes;
            try
            {
                publicKeyBytes = Convert.FromBase64String(base64Key);
            }
            catch (FormatException)
            {
                throw new Exception("Public key is not a valid Base64-encoded string.");
            }

            // ✅ Import Key Properly
            rsaEncryptor.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            // ✅ Validate Key Size (Must Be At Least 2048 Bits)
            if (rsaEncryptor.KeySize < 2048)
            {
                throw new Exception("RSA key size is too small. Ensure the key is at least 2048 bits.");
            }

            // ✅ Encrypt with RSA-OAEP SHA-256
            byte[] encryptedData = rsaEncryptor.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encryptedData);
        }
        catch (Exception ex)
        {
            throw new Exception($"Encryption failed: {ex.Message}", ex);
        }
    }



    // ✅ Decrypt Data using Private Key
    public static string Decrypt(string encryptedData, string encryptedKey)
    {
        if (rsa == null)
            throw new Exception("Private key not loaded!");

        try
        {
            // ✅ Decode Base64
            byte[] encryptedDataBytes = Convert.FromBase64String(encryptedData);
            byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKey);

            // ✅ Decrypt AES Key Using RSA
            byte[] aesKey = rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);

            // ✅ Decrypt Data Using AES
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = aesKey;
            aes.GenerateIV();

            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedDataBytes, 0, encryptedDataBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new Exception("RSA/AES decryption failed.", ex);
        }
    }
    public static string DecryptAESKey(string encryptedKey)
    {
        if (rsa == null)
            throw new Exception("Private key not loaded!");

        try
        {
            byte[] encryptedKeyBytes = Convert.FromBase64String(encryptedKey);
            byte[] aesKey = rsa.Decrypt(encryptedKeyBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(aesKey); // ✅ Convert back to Base64 for easy handling
        }
        catch (CryptographicException ex)
        {
            throw new Exception("RSA decryption of AES key failed.", ex);
        }
    }

    // ✅ Decrypt the AES-encrypted data using the AES key
    public static string DecryptAES(string encryptedData, byte[] aesKey, byte[] iv)
    {
        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7; // <-- Add this explicitly for safety

            using var decryptor = aes.CreateDecryptor();
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            throw new Exception("AES decryption failed. Ensure correct IV and key are used.", ex);
        }
    }


    public static (string encryptedData, string rawAesKey, string encryptedIV) EncryptHybridWithRawKey(string plaintext)
    {
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] encryptedDataBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

            string encryptedData = Convert.ToBase64String(encryptedDataBytes);
            string rawAesKey = Convert.ToBase64String(aes.Key);  // ✅ Send this to FE
            string encryptedIV = Convert.ToBase64String(aes.IV);

            return (encryptedData, rawAesKey, encryptedIV);
        }
        catch (Exception ex)
        {
            throw new Exception($"Encryption failed: {ex.Message}", ex);
        }
    }

}
