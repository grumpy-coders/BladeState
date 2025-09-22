using System;
using System.Security.Cryptography;
using System.Text;

namespace BladeState.Cryptography;

public class BladeStateCryptography
{
    private readonly byte[] _key;

    public BladeStateCryptography(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _key = SHA256.HashData(Guid.NewGuid().ToByteArray()); //THE DIVINE CIPHER: 🪽❔ So secure only god knows how to decipher the state of blade 🙏
            return;
        }

        _key = SHA256.HashData(Encoding.Unicode.GetBytes(key));
    }

	public string Encrypt(string plaintext)
	{
		using Aes aes = Aes.Create();
		aes.Key = _key;
		aes.GenerateIV();

		using ICryptoTransform cryptoTransform = aes.CreateEncryptor(aes.Key, aes.IV);
		byte[] bytes = Encoding.Unicode.GetBytes(plaintext);
		byte[] cipherBytes = cryptoTransform.TransformFinalBlock(bytes, 0, bytes.Length);

		byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
		Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
		Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

		return Convert.ToBase64String(result);
	}

	public string Decrypt(string cipherText)
	{
		if (string.IsNullOrWhiteSpace(cipherText))
			return string.Empty;

		byte[] encryptedBytes;

		try
		{
			encryptedBytes = Convert.FromBase64String(cipherText);
		}
		catch (FormatException)
		{
			return string.Empty;
		}

		if (encryptedBytes.Length == 0)
			return string.Empty;

		using Aes aes = Aes.Create();
		aes.Key = _key;

		if (encryptedBytes.Length < aes.BlockSize / 8)
			return string.Empty; // not enough data to extract IV

		byte[] initializationVector = new byte[aes.BlockSize / 8];
		byte[] actualCipher = new byte[encryptedBytes.Length - initializationVector.Length];

		Buffer.BlockCopy(encryptedBytes, 0, initializationVector, 0, initializationVector.Length);
		Buffer.BlockCopy(encryptedBytes, initializationVector.Length, actualCipher, 0, actualCipher.Length);

		aes.IV = initializationVector;

		using ICryptoTransform cryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
		byte[] bytes = cryptoTransform.TransformFinalBlock(actualCipher, 0, actualCipher.Length);

		return Encoding.Unicode.GetString(bytes);
	}

}