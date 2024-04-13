using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Shared;

public static class SecureHash
{
	/// <summary>
	/// Compute SHA256 hash of passed string.
	/// </summary>
	/// <see cref="http://www.fileformat.info/tool/hash.htm"/>
	/// <see cref="http://stackoverflow.com/questions/18828808/calculating-sha1-from-hex-binary-string-in-c-sharp"/>
	/// <see cref="https://gist.github.com/kristopherjohnson/3021045"/>
	/// <see cref="https://hash.online-convert.com/sha1-generator" />
	/// <remarks>
	/// SHA256 provides stronger security than SHA1.
	/// </remarks>
	/// <example>
	/// string inputString = "Hello, World!";
	/// string sha256Hash = ComputeSHA256Hash(inputString);
	/// </example>
	/// <param name="input">String to be hashed</param>
	/// <returns>String of hex bytes representing the SHA256 hash</returns>
	public static string ComputeSHA256Hash(string input)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(input);

		// Convert the string into bytes
		byte[] inputBytes = Encoding.UTF8.GetBytes(input);

		// Hash the bytes
		byte[] hashBytes = SHA256.HashData(inputBytes);

		return JoinBytes(hashBytes);
	}

	/// <summary>
	/// Compute the SHA256 hash of the passed file.
	/// </summary>
	/// <example>
	/// string sha256Hash = ComputeSHA256HashFile(@"C:\Path\To\File.txt");
	/// </example>
	/// <param name="pathFile">Path to the file to be hashed</param>
	/// <returns>String of hex bytes representing the SHA256 hash</returns>
	public static string ComputeSHA256HashFile(string pathFile)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(pathFile);

		FileInfo fileInfo = new(pathFile);

		using FileStream fileStream = fileInfo.Open(new FileStreamOptions() { Mode = FileMode.Open, Access = FileAccess.Read, });
		try
		{
			// Create a fileStream for the file.
			// Be sure it's positioned to the beginning of the stream.
			fileStream.Position = 0;

			// Hash the file stream
			byte[] hashBytes = SHA256.HashData(fileStream);

			return JoinBytes(hashBytes);
		}
		catch (IOException)
		{
			return string.Empty;
		}
		catch (UnauthorizedAccessException)
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Join bytes into a string.
	/// </summary>
	/// <example>
	/// [ 0x12, 0x34, 0x56 ] => "123456"
	/// </example>
	/// <param name="inBytes">Array of bytes to join</param>
	/// <returns>String consisting of appended bytes</returns>
	public static string JoinBytes(byte[] inBytes)
	{
		StringBuilder stringBuilder = new();
		foreach (byte b in inBytes)
		{
			stringBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}
}
