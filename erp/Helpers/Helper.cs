using Common.Utilities;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Helpers
{
    public static class Helper
    {
        // password format: password + userName [email, phone, username]
        // Verify password: hashed and compare.
        public static string HashedPassword(string password)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-2.1
            // generate a 128-bit salt using a secure PRNG
            //byte[] salt = new byte[128 / 8];
            //using (var rng = RandomNumberGenerator.Create())
            //{
            //    rng.GetBytes(salt);
            //}
            //Console.WriteLine($"Salt: {Convert.ToBase64String(salt)}");

            byte[] salt = Encoding.ASCII.GetBytes("0602198809101991");

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return hashed;
        }
    }
}
