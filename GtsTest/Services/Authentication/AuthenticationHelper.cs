// Services/AuthenticationHelper.cs— 密码哈希工具（PBKDF2）
using System;
using System.Security.Cryptography;
using System.Text;

namespace GtsTest.Services.Authentication
{
    /// <summary>
    /// 密码哈希工具（PBKDF2-SHA256）。提供哈希、验证、升级检测与初始密码生成。
    /// 存储格式（字符串）：
    ///   pbkdf2_sha256$<iterations>$<saltBase64>$<hashBase64>
    /// </summary>
    public static class AuthenticationHelper
    {
        public const int DefaultSaltSize = 16;    // bytes
        public const int DefaultKeySize = 32;     // bytes
        public const int DefaultIterations = 150_000;
        public static int CurrentIterations { get; set; } = DefaultIterations;
        private const string Prefix = "pbkdf2_sha256";

        public static string HashPassword(string password, int? iterations = null)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            int iter = iterations ?? CurrentIterations;

            byte[] salt = RandomNumberGenerator.GetBytes(DefaultSaltSize);
            byte[] hash = PBKDF2(password, salt, iter, DefaultKeySize);

            string saltB64 = Convert.ToBase64String(salt);
            string hashB64 = Convert.ToBase64String(hash);

            return $"{Prefix}${iter}${saltB64}${hashB64}";
        }

        public static bool VerifyPassword(string password, string storedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            var parts = storedHash.Split('$');
            if (parts.Length != 4) return false;
            if (!string.Equals(parts[0], Prefix, StringComparison.Ordinal)) return false;

            if (!int.TryParse(parts[1], out int iterations)) return false;
            byte[] salt, expectedHash;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expectedHash = Convert.FromBase64String(parts[3]);
            }
            catch
            {
                return false;
            }

            byte[] actualHash = PBKDF2(password, salt, iterations, expectedHash.Length);

            bool verified = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);

            if (verified && iterations < CurrentIterations)
                needsRehash = true;

            return verified;
        }

        public static (string PlainPassword, string PasswordHash) CreateInitialPassword(int length = 12)
        {
            if (length < 8) length = 12;
            string pwd = GenerateSecureRandomPassword(length);
            string hash = HashPassword(pwd);
            return (pwd, hash);
        }

        public static bool NeedsRehash(string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return true;
            var parts = storedHash.Split('$');
            if (parts.Length != 4) return true;
            if (!int.TryParse(parts[1], out int iterations)) return true;
            return iterations < CurrentIterations;
        }

        private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
        {
            var pepper = GetPepperBytes();
            byte[] pwdBytes = Encoding.UTF8.GetBytes(password);
            byte[] pwdPepper = pepper == null || pepper.Length == 0 ? pwdBytes : Concat(pwdBytes, pepper);

            using var kdf = new Rfc2898DeriveBytes(pwdPepper, salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(outputBytes);
        }

        private static string GenerateSecureRandomPassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789@_-";
            var bytes = RandomNumberGenerator.GetBytes(length);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(chars[bytes[i] % chars.Length]);
            return sb.ToString();
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var outb = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, outb, 0, a.Length);
            Buffer.BlockCopy(b, 0, outb, a.Length, b.Length);
            return outb;
        }

        private static byte[]? GetPepperBytes()
        {
            try
            {
                var pepper = Environment.GetEnvironmentVariable("GTS_PASSWORD_PEPPER");
                if (string.IsNullOrEmpty(pepper)) return null;
                return Encoding.UTF8.GetBytes(pepper);
            }
            catch
            {
                return null;
            }
        }
    }
}