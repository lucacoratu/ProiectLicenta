using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;

namespace EndToEndWillow {
    public static class Encryption {
        public static ECCurve Curve25519 { get; } = new ECCurve() 
        {
            CurveType = ECCurve.ECCurveType.PrimeMontgomery,
            B = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            A = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x07, 0x6d, 0x06 }, // 486662
            G = new ECPoint() {
                X = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 9 },
                Y = new byte[] { 0x20, 0xae, 0x19, 0xa1, 0xb8, 0xa0, 0x86, 0xb4, 0xe0, 0x1e, 0xdd, 0x2c, 0x77, 0x48, 0xd1, 0x4c, 0x92, 0x3d, 0x4d, 0x7e, 0x6d, 0x7c, 0x61, 0xb2, 0x29, 0xe9, 0xc5, 0xa2, 0x7e, 0xce, 0xd3, 0xd9 }
            },
            Prime = new byte[] { 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xed },
            //Prime = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            Order = new byte[] { 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0xde, 0xf9, 0xde, 0xa2, 0xf7, 0x9c, 0xd6, 0x58, 0x12, 0x63, 0x1a, 0x5c, 0xf5, 0xd3, 0xed },
            Cofactor = new byte[] { 8 }
        };

        public static ECDiffieHellman GenerateX25519Key() {
            //ECDiffieHellman instance = ECDiffieHellman.Create(Curve25519);
            ECDiffieHellman instance = ECDiffieHellman.Create();
            //Console.WriteLine(instance.ExportECPrivateKeyPem());
            //Console.WriteLine(instance.PublicKey.ExportSubjectPublicKeyInfo());
            return instance;
        }

        public static byte[] EncryptAES256CBC(string message, byte[] key, byte[] iv) {
            byte[] array;

            using (Aes aes = Aes.Create()) {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream()) {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write)) {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream)) {
                            streamWriter.Write(message);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return array;
        }

        public static string DecryptAES256CBC(byte[] cipherText, byte[] key, byte[] iv) {
            // Create AesManaged
            string plainText;
            using (Aes aes = Aes.Create()) {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;

                // Create a decryptor
                ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                // Create the streams used for decryption.
                using (MemoryStream ms = new MemoryStream(cipherText)) {
                    // Create crypto stream
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                        // Read crypto stream
                        using (StreamReader reader = new StreamReader(cs))
                            plainText = reader.ReadToEnd();
                    }
                }
            }

            return plainText;
        }
    }
}
