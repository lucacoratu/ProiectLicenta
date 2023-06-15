using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using WillowClient.Model;
using System.Security.AccessControl;

namespace WillowClient.Encryption {
    public static class Utils {
        // This is the X25519 curve
        public static ECCurve Curve25519 { get; } = new ECCurve() {
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

        //The hashing algorithm used in the application
        public static HashAlgorithmName diffieHellmanHashingAlgorithm = HashAlgorithmName.SHA256;

        //The bytes which will be used in the HMAC-SHA256 function to ratchet the message key
        private static byte[] messageKeyBytes = { 0x01 };

        //The bytes which will be used in the HMAC-SHA256 function to ratchet the chain key
        private static byte[] chainKeyBytes = { 0x02 };

        //Generate a new ECDH private - public key pair
        public static ECDiffieHellman GenerateX25519Key() {
            //ECDiffieHellman instance = ECDiffieHellman.Create(Curve25519);
            ECDiffieHellman instance = ECDiffieHellman.Create();
            //Console.WriteLine(instance.ExportECPrivateKeyPem());
            //Console.WriteLine(instance.PublicKey.ExportSubjectPublicKeyInfo());
            return instance;
        }

        //Signs a piece of data specified by the user and by using the private key in PEM format
        //Returns the data base64 encoded
        public static string ECDSASignData(string data, string privateKey) {
            ECDsa dsa = ECDsa.Create();
            dsa.ImportFromPem(privateKey);

            var result = dsa.SignData(Encoding.UTF8.GetBytes(data), HashAlgorithmName.SHA256);
            return System.Convert.ToBase64String(result);
        }

        private static byte[] Combine(params byte[][] arrays) {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays) {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        //Compute initiator master secret
        public static byte[] ComputeSenderMasterSecret(string ephemeralPrivate, string friendIdentityPub, string friendPreSignedPub) {
            var friendIdentity = GenerateX25519Key();
            var friendPreSigned = GenerateX25519Key();
            var ephemeralKeyPair = GenerateX25519Key(); 
            friendIdentity.ImportFromPem(friendIdentityPub);
            friendPreSigned.ImportFromPem(friendPreSignedPub);
            ephemeralKeyPair.ImportFromPem(ephemeralPrivate);
            var first = Globals.identityKey.DeriveKeyFromHash(friendPreSigned.PublicKey, diffieHellmanHashingAlgorithm);
            var second = ephemeralKeyPair.DeriveKeyFromHash(friendIdentity.PublicKey, diffieHellmanHashingAlgorithm);
            var third = ephemeralKeyPair.DeriveKeyFromHash(friendPreSigned.PublicKey, diffieHellmanHashingAlgorithm);
            byte[][] parts = {first, second, third};
            var masterSecret = Combine(parts);
            return masterSecret;
        }

        //Compute the receiver master secret
        public static byte[] ComputeReceiverMasterSecret(string friendIdentityPublic, string friendEphemeralPublic) {
            var friendIdentityPubKey = GenerateX25519Key();
            var friendEphemeralPubKey = GenerateX25519Key();
            friendIdentityPubKey.ImportFromPem(friendIdentityPublic);
            friendEphemeralPubKey.ImportFromPem(friendEphemeralPublic);

            var first = Globals.preSignedKey.DeriveKeyFromHash(friendIdentityPubKey.PublicKey, diffieHellmanHashingAlgorithm);
            var second = Globals.identityKey.DeriveKeyFromHash(friendEphemeralPubKey.PublicKey, diffieHellmanHashingAlgorithm);
            var third = Globals.preSignedKey.DeriveKeyFromHash(friendEphemeralPubKey.PublicKey, diffieHellmanHashingAlgorithm);
            byte[][] parts = { first, second, third };
            var masterSecret = Combine(parts);
            return masterSecret;
        }

        //Compute the chain key and the root key from the master secret
        public static RootChainKeys GenerateRootAndChainKeyFromMasterSecret(byte[] masterSecret) {
            var rootAndChainBlob = HKDF.DeriveKey(diffieHellmanHashingAlgorithm, masterSecret, 64);
            var chainRootKeys = new RootChainKeys();
            chainRootKeys.RootKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 0, chainRootKeys.RootKey, 0, 32);
            chainRootKeys.ChainKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 32, chainRootKeys.ChainKey, 0, 32);
            return chainRootKeys;
        }

        //Compute the ephemeral secret when the sender is changed
        public static byte[] ComputeEphemeralSecret(ECDiffieHellman userEphemeralPrivate, ECDiffieHellman senderEphemeralPublic) {
            var ephemeralSecret = userEphemeralPrivate.DeriveKeyFromHash(senderEphemeralPublic.PublicKey, diffieHellmanHashingAlgorithm);
            return ephemeralSecret;
        }

        //Compute the root and chain keys from the ephemeral secret
        public static RootChainKeys ComputeRootChainFromEphemeralSecret(byte[] ephemeralSecret, byte[] previousRootKey) {
            var rootAndChainBlob = HKDF.DeriveKey(diffieHellmanHashingAlgorithm, previousRootKey, 64, ephemeralSecret);
            RootChainKeys keys = new RootChainKeys();
            keys.RootKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 0, keys.RootKey, 0, 32);
            keys.ChainKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 32, keys.ChainKey, 0, 32);
            return keys;
        }

        public static byte[] UpdateChainKey(byte[] chainKey) {
            return HMACSHA256.HashData(chainKey, chainKeyBytes);
        }

        public static MessageChainKeys GenerateMessageKey(byte[] chainKey) {
            var firstPart = HMACSHA256.HashData(chainKey, messageKeyBytes);
            chainKey = UpdateChainKey(chainKey);
            var secondPart = HMACSHA256.HashData(chainKey, messageKeyBytes);
            chainKey = UpdateChainKey(chainKey);
            var thirdPart = HMACSHA256.HashData(chainKey, messageKeyBytes);
            chainKey = UpdateChainKey(chainKey);
            byte[] third = new byte[thirdPart.Length / 2];
            System.Buffer.BlockCopy(thirdPart, 0, third, 0, thirdPart.Length / 2);
            byte[][] parts = { firstPart, secondPart, third };

            var messageChainKeys = new MessageChainKeys();
            messageChainKeys.ChainKey = chainKey;
            messageChainKeys.MessageKey = Combine(parts);
            return messageChainKeys;
        }

        //Encrypt using AES256CBC the message received using the key with the specified iv
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

        public static byte[] EncryptBlobData(Stream dataStream, byte[] key, byte[] iv) {
            byte[] array;
            var ms = new MemoryStream();
            dataStream.CopyTo(ms);
            var data = ms.ToArray();

            using (Aes aes = Aes.Create()) {
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream memoryStream = new MemoryStream()) {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)) {
                        cryptoStream.Write(data, 0, data.Length);
                        cryptoStream.FlushFinalBlock();
                        array = memoryStream.ToArray();
                    }
                }
            }

            return array;
        }

        public static byte[] DecryptBlobData(byte[] blobData, byte[] key, byte[] iv) {
            byte[] blob = new byte[blobData.Length - 32];
            System.Buffer.BlockCopy(blobData, 0, blob, 0, blobData.Length - 32);

            var plainText = default(byte[]);
            using (Aes aes = Aes.Create()) {
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                // Create a decryptor
                ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                // Create the streams used for decryption.
                using (MemoryStream ms = new MemoryStream(blob)) {
                    // Create crypto stream
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read)) {
                        using(MemoryStream outputStream = new MemoryStream()) {
                            cs.CopyTo(outputStream);

                            plainText = outputStream.ToArray();
                        }
                    }
                }
            }

            return plainText;
        }

        public static byte[] AutheticateBlobData(byte[] encryptedData, byte[] key) {
            return HMACSHA256.HashData(key, encryptedData);
        }

        public static byte[] HashBlobCipherText(byte[] blob) {
            return SHA256.HashData(blob);
        }

        public static string EncryptMessage(string message, byte[] messageKey) {
            //First 32 bytes are used for the AES-256CBC key
            byte[] aesKey = new byte[32];
            System.Buffer.BlockCopy(messageKey, 0, aesKey, 0, 32);
            //Bytes from 32 - 64 are used for the HMAC-SHA256 key
            byte[] hmacKey = new byte[32];
            System.Buffer.BlockCopy(messageKey, aesKey.Length, hmacKey, 0, 32);
            //Last 16 bytes are used as IV for AES-256CBC
            byte[] aesIv = new byte[16];
            System.Buffer.BlockCopy(messageKey, aesKey.Length + hmacKey.Length, aesIv, 0, 16);

            //Encrypt the message using AES256CBC
            var encMessage = EncryptAES256CBC(message, aesKey, aesIv);
            //Create the authentication blob
            var authenticationBlob = HMACSHA256.HashData(encMessage, hmacKey);
            byte[] blob = new byte[encMessage.Length + authenticationBlob.Length];
            //Copy the encrypted message to the blob
            System.Buffer.BlockCopy(encMessage, 0, blob, 0, encMessage.Length);
            //Copy the authentication tag to the blob
            System.Buffer.BlockCopy(authenticationBlob, 0, blob, encMessage.Length, authenticationBlob.Length);
            //Return the encrypted message and the authentication tag base64 encoded
            return System.Convert.ToBase64String(blob);
        }

        //Decrypt the ciphertext using the key and the iv specified
        public static string DecryptAES256CBC(byte[] cipherText, byte[] key, byte[] iv) {
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

        public static string DecryptMessage(string encMessage, byte[] messageKey) {
            //First 32 bytes are used for the AES-256CBC key
            byte[] aesKey = new byte[32];
            System.Buffer.BlockCopy(messageKey, 0, aesKey, 0, 32);
            //Bytes from 32 - 64 are used for the HMAC-SHA256 key
            byte[] hmacKey = new byte[32];
            System.Buffer.BlockCopy(messageKey, aesKey.Length, hmacKey, 0, 32);
            //Last 16 bytes are used as IV for AES-256CBC
            byte[] aesIv = new byte[16];
            System.Buffer.BlockCopy(messageKey, aesKey.Length + hmacKey.Length, aesIv, 0, 16);

            byte[] blob = System.Convert.FromBase64String(encMessage);
            byte[] messageData = new byte[blob.Length - 32];
            System.Buffer.BlockCopy(blob, 0, messageData, 0, blob.Length - 32);

            //TO DO verify the hmac tag...

            return DecryptAES256CBC(messageData, aesKey, aesIv);
        }
    }
}
