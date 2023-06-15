#define DEBUG_MODE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EndToEndWillow {
    public class PeerModel {
        //The id of the peer
        public int Id { get; set; }
        //The name of the peer
        public string? Name { get; set; }
        //The public ephemeral key of the peer if exists
        public ECDiffieHellmanPublicKey? peerEphemeralPublicKey { get; set; }
        //The public identity key of the peer
        public ECDiffieHellmanPublicKey? peerIdentityPublicKey { get; set; }
        //The public signed pre public key of the peer
        public ECDiffieHellmanPublicKey? peerSignedPrePublicKey { get; set; }
        //The public one time public key of the peer
        public ECDiffieHellmanPublicKey? peerOneTimePublicKey { get; set; }
        //The master secret generated for the user
        public byte[]? MasterSecret { get; set; }
        //The ephemeral secret generated when the user responds
        public byte[]? EphemeralSecret { get; set; }
        //The chain key
        public byte[]? ChainKey { get; set; }
        //The root key
        public byte[]? RootKey { get; set; }
        //The ephemeral key of the current user
        public ECDiffieHellman? ephemeralKeyPair { get; set; }
        //The identity key of the current user
        public ECDiffieHellman? identityKeyPair { get; set; }
        //The signed pre key of the current user
        public ECDiffieHellman? signedPreKeyPair { get; set; }
        public ECDiffieHellman? oneTimeKeyPair { get; set; }

        //The bytes which will be used in the HMAC-SHA256 function to ratchet the message key
        private byte[] messageKeyBytes = { 0x01 };

        //The bytes which will be used in the HMAC-SHA256 function to ratchet the chain key
        private byte[] chainKeyBytes = { 0x02 };

        //The hashing algorithm which will be used when derivating the keys
        private HashAlgorithmName diffieHellmanHashingAlgorithm = HashAlgorithmName.SHA256;

        //The name of the sender
        public string? SenderName { get; set; }

        public bool CanGenerateMasterSecret() {
            if (MasterSecret != null)
                return false;
            if (RootKey != null)
                return false;
            if(ChainKey != null)
                return false;

            if (peerIdentityPublicKey == null || peerSignedPrePublicKey == null)
                return false;
            
            return true;
        }

        private byte[] Combine(params byte[][] arrays) {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays) {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private byte[]? GenerateSenderMasterSecret() {
            if (identityKeyPair == null || ephemeralKeyPair == null)
                return null;

            if (peerIdentityPublicKey == null || peerSignedPrePublicKey == null)
                return null;

            var first = identityKeyPair.DeriveKeyFromHash(this.peerSignedPrePublicKey, diffieHellmanHashingAlgorithm);
            var second = ephemeralKeyPair.DeriveKeyFromHash(this.peerIdentityPublicKey, diffieHellmanHashingAlgorithm);
            var third = ephemeralKeyPair.DeriveKeyFromHash(this.peerSignedPrePublicKey, diffieHellmanHashingAlgorithm);
            var forth = ephemeralKeyPair.DeriveKeyFromHash(this.peerOneTimePublicKey, diffieHellmanHashingAlgorithm);
            byte[][] parts = { first, second, third, forth };
            return this.Combine(parts);
        }

        private void GenerateChainAndRootKey() {
            if (this.RootKey != null)
                return;
            if (this.ChainKey != null)
                return;
            if (this.MasterSecret == null)
                return;

            var rootAndChainBlob = HKDF.DeriveKey(diffieHellmanHashingAlgorithm, this.MasterSecret, 64);
            this.RootKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 0, this.RootKey, 0, 32);
            this.ChainKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 32, this.ChainKey, 0, 32);
        }

        public void UpdateChainKey() {
            this.ChainKey = HMACSHA256.HashData(this.ChainKey, this.chainKeyBytes);
#if DEBUG_MODE
            Console.WriteLine($"[*] {this.SenderName} -> Chain key updated -> Conversation with User: {this.Name}");
            foreach(var value in this.ChainKey) {
                Console.Write(value.ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine("=====================================");
#endif
        }

        public byte[] GenerateMessageKey() {
            var firstPart = HMACSHA256.HashData(this.ChainKey, this.messageKeyBytes);
            this.UpdateChainKey();
            var secondPart = HMACSHA256.HashData(this.ChainKey, this.messageKeyBytes);
            this.UpdateChainKey();
            var thirdPart = HMACSHA256.HashData(this.ChainKey, this.messageKeyBytes);
            this.UpdateChainKey();
            byte[] third = new byte[thirdPart.Length / 2];
            System.Buffer.BlockCopy(thirdPart, 0, third, 0, thirdPart.Length / 2);
            byte[][] parts = { firstPart, secondPart, third };
            return this.Combine(parts);
        }

        //public byte[] CreateEncryptedMessageBlob(string message, byte[] messageKey) {
        //    //First 32 bytes are used for the AES-256CBC key
        //    byte[] aesKey = new byte[32];
        //    System.Buffer.BlockCopy(messageKey, 0, aesKey, 0, 32);
        //    //Bytes from 32 - 64 are used for the HMAC-SHA256 key
        //    byte[] hmacKey = new byte[32];
        //    System.Buffer.BlockCopy(messageKey, aesKey.Length, hmacKey, 0, 32);
        //    //Last 16 bytes are used as IV for AES-256CBC
        //    byte[] aesIv = new byte[16];
        //    System.Buffer.BlockCopy(messageKey, aesKey.Length + hmacKey.Length, aesIv, 0, 16);

        //    //Encrypt the message using AES256CBC
        //    var encMessage = Encryption.EncryptAES256CBC(message, aesKey, aesIv);
        //    //Create the authentication blob
        //    var authenticationBlob = HMACSHA256.HashData(encMessage, hmacKey);
        //    byte[] blob = new byte[encMessage.Length + authenticationBlob.Length];
        //    //Copy the encrypted message to the blob
        //    System.Buffer.BlockCopy(encMessage, 0, blob, 0, encMessage.Length);
        //    //Copy the authentication tag to the blob
        //    System.Buffer.BlockCopy(authenticationBlob, 0, blob, encMessage.Length, authenticationBlob.Length);
        //    return blob;
        //}

        public void UpdateChainKeyAndRootKeyAfterChangingSender() {
            this.EphemeralSecret = this.ephemeralKeyPair.DeriveKeyFromHash(this.peerEphemeralPublicKey, this.diffieHellmanHashingAlgorithm);
#if DEBUG_MODE
            Console.WriteLine($"[*] {this.SenderName} -> Ephemeral secret updated -> Conversation with User: {this.Name}");
            foreach (var value in this.EphemeralSecret) {
                Console.Write(value.ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine("=====================================");
#endif
            var rootAndChainBlob = HKDF.DeriveKey(diffieHellmanHashingAlgorithm, this.RootKey, 64, this.EphemeralSecret);
            //this.RootKey = new byte[32];
#if DEBUG_MODE
            System.Buffer.BlockCopy(rootAndChainBlob, 0, this.RootKey, 0, 32);
            Console.WriteLine($"[*] {this.SenderName} -> Root key updated -> Conversation with User: {this.Name}");
            foreach (var value in this.RootKey) {
                Console.Write(value.ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine("=====================================");
#endif

            //this.ChainKey = new byte[32];
            System.Buffer.BlockCopy(rootAndChainBlob, 32, this.ChainKey, 0, 32);
#if DEBUG_MODE
            Console.WriteLine($"[*] {this.SenderName} -> Chain key updated -> Conversation with User: {this.Name}");
            foreach (var value in this.ChainKey) {
                Console.Write(value.ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine("=====================================");
#endif
        }

        public void GenerateMasterSecret() {
            this.MasterSecret = this.GenerateSenderMasterSecret();
        }

        private byte[]? GenerateRecepientMasterSecret() {
            if (this.peerIdentityPublicKey == null || peerEphemeralPublicKey == null)
                return null;

            if (this.signedPreKeyPair == null || this.identityKeyPair == null || this.oneTimeKeyPair == null)
                return null;

            var first = this.signedPreKeyPair.DeriveKeyFromHash(peerIdentityPublicKey, diffieHellmanHashingAlgorithm);
            var second = this.identityKeyPair.DeriveKeyFromHash(peerEphemeralPublicKey, diffieHellmanHashingAlgorithm);
            var third = this.signedPreKeyPair.DeriveKeyFromHash(peerEphemeralPublicKey, diffieHellmanHashingAlgorithm);
            var forth = this.oneTimeKeyPair.DeriveKeyFromHash(peerEphemeralPublicKey, diffieHellmanHashingAlgorithm);
            byte[][] parts = { first, second, third, forth };
            return this.Combine(parts);
        }
        public void GenerateReceiverMasterSecret() {
            this.MasterSecret = this.GenerateRecepientMasterSecret();
            this.GenerateChainAndRootKey();
        }

        public void InitializeSecureConnection() {
            this.GenerateMasterSecret();
            this.GenerateChainAndRootKey();
        }
    }
}
