using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseConnection {
    class Util {
        public static string HashString(string text) {
            const string chars = "0123456789abcdefghijklmnopqrztuvABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);

            char[] hash2 = new char[32];

            // Note that here we are wasting bits of hash! 
            // But it isn't really important, because hash.Length == 32
            for (int i = 0; i < hash2.Length; i++) {
                hash2[i] = chars[hash[i] % chars.Length];
            }

            return new string(hash2);
        }
    }
}
