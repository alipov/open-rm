using System.IO;
using System.Text;
using System.Security.Cryptography;

// Performs symmetric encryption and decryption using AES (Advanced Encryption Standard) algorithm.

namespace OpenRm.Common.Entities
{
    public static class EncryptionAdapter
    {
        private const string iv = "H03vh9at$ppw5#sa"; // hardcoded

        private static byte[] keyBytes, ivBytes; 

        private static bool _encryptionEnabled = true;

        static Encoding utf8 = new UTF8Encoding(false);

        private static readonly object lck = new object();     // for handeling many threads

        public static void SetEncryption(string key)
        {
            if (key == "")
                // empty key means disable encryption
                _encryptionEnabled = false;
            else
            {
                keyBytes = utf8.GetBytes(key);
                ivBytes = utf8.GetBytes(iv);

                
            }
        }


        public static byte[] Encrypt(byte[] text)
        {
            if (!_encryptionEnabled)
            {
                return text;   //return text back
            }

            string plainText = utf8.GetString(text);

            // Create an AesCryptoServiceProvider object with the specified key and IV.
            using (var aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;

                // Create an encrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] encrypted;

                // Create the streams used for encryption.
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }

                return encrypted;    //return encrypted data    

            }

        }


        public static byte[] Decrypt(byte[] cipherText)
        {
            if (!_encryptionEnabled)
            {
                return cipherText;
            }

            byte[] decrypted;

            // Create an AesCryptoServiceProvider object with the specified key and IV.
            using (var aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (var msDecrypt = new MemoryStream(cipherText))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            decrypted = utf8.GetBytes(srDecrypt.ReadToEnd());
                        }
                    }
                }

                return decrypted;

            }

        }
    }
}



















//using System.IO;
//using System.Text;
//using System.Security.Cryptography;

//// Performs symmetric encryption and decryption using AES (Advanced Encryption Standard) algorithm.

//namespace OpenRm.Common.Entities
//{
//    public static class EncryptionAdapter
//    {
//        private const string iv = "H03vh9at$ppw5#sa"; // hardcoded

//        private static ICryptoTransform encryptor;
//        private static ICryptoTransform decryptor;

//        private static bool _encryptionEnabled = true;

//        static Encoding utf8 = new UTF8Encoding(false);

//        private static readonly object lck = new object();     // for handeling many threads

//        public static void SetEncryption(string key)
//        {
//            if (key == "")
//                // empty key means disable encryption
//                _encryptionEnabled = false;
//            else
//            {
//                byte[] keyBytes = utf8.GetBytes(key);
//                byte[] ivBytes = utf8.GetBytes(iv);

//                // Create an AesCryptoServiceProvider object with the specified key and IV.
//                using (var aesAlg = new AesCryptoServiceProvider())
//                {
//                    aesAlg.Key = keyBytes;
//                    aesAlg.IV = ivBytes;

//                    // Create an encrytor to perform the stream transform.
//                    encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

//                    // Create a decrytor to perform the stream transform.
//                    decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
//                }
//            }
//        }


//        public static byte[] Encrypt(byte[] text)
//        {
//            if (!_encryptionEnabled)
//            {
//                return text;   //return text back
//            }

//            string plainText = utf8.GetString(text);

//            byte[] encrypted;

//            // Create the streams used for encryption.
//            using (var msEncrypt = new MemoryStream())
//            {
//                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
//                {
//                    using (var swEncrypt = new StreamWriter(csEncrypt))
//                    {
//                        swEncrypt.Write(plainText);
//                    }
//                    encrypted = msEncrypt.ToArray();
//                }
//            }

//            return encrypted;    //return encrypted data    
            
//        }


//        public static byte[] Decrypt(byte[] cipherText)
//        {
//            if (!_encryptionEnabled)
//            {
//                return cipherText;
//            }

//            byte[] decrypted;

//            // Create the streams used for decryption.
//            using (var msDecrypt = new MemoryStream(cipherText))
//            {
//                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
//                {
//                    using (var srDecrypt = new StreamReader(csDecrypt))
//                    {
//                        decrypted = utf8.GetBytes(srDecrypt.ReadToEnd());
//                    }
//                }
//            }

//            return decrypted;
            
//        }
//    }
//}
