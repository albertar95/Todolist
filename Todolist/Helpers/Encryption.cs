using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Todolist.Helpers
{
    public class Encryption
    {
        private static string PrivateKeyPath = HttpContext.Current.Server.MapPath("~/App_Data/privatekey");
        private static string PublicKey = ConfigurationManager.AppSettings["PublicKey"];
        public static string DecryptString(string cipher)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                using (var tdes = new TripleDESCryptoServiceProvider())
                {
                    tdes.Key = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes("B@8CCto%YgfBF8OP1!Con007W"));
                    tdes.Mode = CipherMode.ECB;
                    tdes.Padding = PaddingMode.PKCS7;

                    using (var transform = tdes.CreateDecryptor())
                    {
                        byte[] cipherBytes = Convert.FromBase64String(cipher);
                        byte[] bytes = transform.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return UTF8Encoding.UTF8.GetString(bytes);
                    }
                }
            }
        }
        public static string EncryptString(string Text)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                using (var tdes = new TripleDESCryptoServiceProvider())
                {
                    tdes.Key = md5.ComputeHash(UTF8Encoding.UTF8.GetBytes("B@8CCto%YgfBF8OP1!Con007W"));
                    tdes.Mode = CipherMode.ECB;
                    tdes.Padding = PaddingMode.PKCS7;

                    using (var transform = tdes.CreateEncryptor())
                    {
                        byte[] textBytes = UTF8Encoding.UTF8.GetBytes(Text);
                        byte[] bytes = transform.TransformFinalBlock(textBytes, 0, textBytes.Length);
                        return Convert.ToBase64String(bytes, 0, bytes.Length);
                    }
                }
            }
        }
        public static string RSAEncrypt(string input)
        {
            try
            {
                return EncryptText(input, PublicKey);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public static string RSADecrypt(string input, string password)
        {
            try
            {
                return DecryptText(input, PrivateKeyPath, PublicKey, password);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        private static string EncryptText(string input, string publicKey)
        {
            var publicKeyBytes = new List<byte[]>();
            publicKey.Split(',').ToList().ForEach(x => { publicKeyBytes.Add(Convert.FromBase64String(x)); });
            var rsaParameter = new RSAParameters() { Exponent = publicKeyBytes.First(), Modulus = publicKeyBytes.Last() };
            var csp = new RSACryptoServiceProvider(2048);
            csp.ImportParameters(rsaParameter);
            return Convert.ToBase64String(csp.Encrypt(Encoding.Unicode.GetBytes(input), false));
        }
        private static string DecryptText(string input, string pkPath, string publicKey, string password)
        {
            var fileOrder = password.ToArray().Distinct().ToArray();
            string privateKey = "";
            for (int i = 0; i < 10; i++)
            {
                privateKey += File.ReadAllText(Path.Combine(pkPath, $"{fileOrder[i]}.txt"));
            }
            var privateKeyBytes = new List<byte[]>();
            var publicKeyBytes = new List<byte[]>();
            privateKey.Split(',').ToList().ForEach(x => { privateKeyBytes.Add(Convert.FromBase64String(x)); });
            publicKey.Split(',').ToList().ForEach(x => { publicKeyBytes.Add(Convert.FromBase64String(x)); });
            var rsaParameter = new RSAParameters() { Exponent = publicKeyBytes.First(), Modulus = publicKeyBytes.Last() };
            rsaParameter.D = privateKeyBytes.ToArray()[0];
            rsaParameter.DP = privateKeyBytes.ToArray()[1];
            rsaParameter.DQ = privateKeyBytes.ToArray()[2];
            rsaParameter.InverseQ = privateKeyBytes.ToArray()[3];
            rsaParameter.P = privateKeyBytes.ToArray()[4];
            rsaParameter.Q = privateKeyBytes.ToArray()[5];
            var csp = new RSACryptoServiceProvider(2048);
            csp.ImportParameters(rsaParameter);
            var inputBytes = Convert.FromBase64String(input);
            return Encoding.Unicode.GetString(csp.Decrypt(inputBytes, false));
        }
        public static string Sha256Hash(string rawData)
        {
            // Create a SHA256
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
