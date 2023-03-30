//#define DEBUG_MODE

using Org.BouncyCastle.Operators;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Buffers;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Tls;
using System.Diagnostics;


namespace EndToEndWillow {
    public class UserModel {
        //The id of the account
        private int id;

        public int Id {
            get => id;
        }

        //The name of the account
        private string name;

        public string Name {
            get => name;
        }

        private List<int> roomIds = new();

        public List<int> RoomIds {
            get => roomIds;
        }

        //The identity key pair
        [AllowNull]
        private ECDiffieHellman identityKeyPair = null;

        public ECDiffieHellmanPublicKey IdentityPublicKey {
            get => identityKeyPair.PublicKey;
        }

        //The signed pre key pair
        [AllowNull]
        private ECDiffieHellman signedPreKeyPair = null;

        public ECDiffieHellmanPublicKey SignedPrePublicKey { 
            get => signedPreKeyPair.PublicKey; 
        }

        //The list of one time pre keys
        [AllowNull]
        private List<ECDiffieHellman> oneTimeKeyPair = null;

        public ECDiffieHellmanPublicKey OneTimePublicKey {
            get => oneTimeKeyPair[0].PublicKey;
        }

        [AllowNull]
        private ECDiffieHellman ephemeralInitiator = null;

        private bool shouldChangeEphemeral = true;
        public bool ShouldChangeEphemeral {
            get => shouldChangeEphemeral;
            set => shouldChangeEphemeral = value;
        }

        private List<PeerModel> userPeers = new();

        public ECDiffieHellmanPublicKey EphemeralInitiator { 
            get => ephemeralInitiator.PublicKey; 
        }

        private HashAlgorithmName diffieHellmanHashingAlgorithm = HashAlgorithmName.SHA256;

        private byte[] rootKey = new byte[32];

        public byte[] RootKey {
            get => rootKey;
        }

        private byte[] chainKey = new byte[32];

        public byte[] ChainKey { 
            get => chainKey;
        }

        //The bytes which will be used in the HMAC-SHA256 function to ratchet the message key
        private byte[] messageKeyBytes = { 0x01 };

        //The bytes which will be used in the HMAC-SHA256 function to ratchet the chain key
        private byte[] chainKeyBytes = { 0x02 };

        public UserModel(int id, string name) { 
            this.id = id;
            this.name = name;
        }

        public void AddUserRoomId(int roomId) {
            this.roomIds.Add(roomId);
        }

        //Generate the identity key pair
        public void GenerateIdentityKeyPair() {
            identityKeyPair = Encryption.GenerateX25519Key();
        }

        //Generate the signed pre key
        public void GenerateSignedPreKey() {
            signedPreKeyPair = Encryption.GenerateX25519Key();

        }

        //Generate the list of One time pre keys
        public void GenerateOneTimePreKeys() {
            if (this.oneTimeKeyPair == null)
                this.oneTimeKeyPair = new();
            
            for(int i = 0; i < 8; i++)
                this.oneTimeKeyPair.Add(Encryption.GenerateX25519Key());
        }

        public void GenerateNecessaryKeys() {
            this.GenerateIdentityKeyPair();
            this.GenerateSignedPreKey();
            this.GenerateOneTimePreKeys();
        }

        public byte[] CreateEncryptedMessageBlob(string message, byte[] messageKey) {
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
            var encMessage = Encryption.EncryptAES256CBC(message, aesKey, aesIv);
            //Create the authentication blob
            var authenticationBlob = HMACSHA256.HashData(encMessage, hmacKey);
            byte[] blob = new byte[encMessage.Length + authenticationBlob.Length];
            //Copy the encrypted message to the blob
            System.Buffer.BlockCopy(encMessage, 0, blob, 0, encMessage.Length);
            //Copy the authentication tag to the blob
            System.Buffer.BlockCopy(authenticationBlob, 0, blob, encMessage.Length, authenticationBlob.Length);
            return blob;
        }

        public string DecryptMessageBlob(byte[] encMessage, byte[] messageKey) {
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
            var plainMessage = Encryption.DecryptAES256CBC(encMessage, aesKey, aesIv);
            return plainMessage;
        }

        public MessageModel? EncryptMessageToUser(string message, PeerModel pm, int lastMessageSenderId) {
            //Check if there is a chain key and a root key generated for the user
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool found = false;
            int index = -1;
            for (int i = 0; i < this.userPeers.Count; i++) {
                if (this.userPeers[i].Id == pm.Id) {
                    found = true;
                    index = i;
                    break;
                }
            }
            if (!found) {
                pm.signedPreKeyPair = this.signedPreKeyPair;
                pm.identityKeyPair = this.identityKeyPair;
                pm.ephemeralKeyPair = Encryption.GenerateX25519Key();
                pm.SenderName = this.Name;
                pm.InitializeSecureConnection();
                this.userPeers.Add(pm);
            }

            if (found) {
                if (lastMessageSenderId != this.Id) {
                    //Change the ephemeral key
                    this.userPeers[index].ephemeralKeyPair = Encryption.GenerateX25519Key();
                    this.userPeers[index].UpdateChainKeyAndRootKeyAfterChangingSender();
                }
            }
            if (found) {
                if (this.userPeers[index].MasterSecret == null)
                    return null;
            }

            byte[]? messageKey = null;
            if (!found)
                messageKey = pm.GenerateMessageKey();
            else
                messageKey = this.userPeers[index].GenerateMessageKey();
#if DEBUG_MODE
            Console.WriteLine("Message key");
            foreach (var value in messageKey) {
                Console.Write(value.ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine("Message key");
#endif
            var encMessage = this.CreateEncryptedMessageBlob(message, messageKey);

            MessageModel? msgModel = null;
            if (!found) {
                msgModel = new MessageModel {
                    Id = 0,
                    InitatorIdentityPublicKey = System.Convert.ToBase64String(this.identityKeyPair.ExportSubjectPublicKeyInfo()),
                    InitiatorEphemeralPublicKey = System.Convert.ToBase64String(pm.ephemeralKeyPair.ExportSubjectPublicKeyInfo()),
                    MessageData = System.Convert.ToBase64String(encMessage),
                    Timestamp = DateTime.Now.ToString(),
                    SenderName = this.Name,
                    SenderId = this.id,
                };
            }
            else {
                msgModel = new MessageModel {
                    Id = 0,
                    InitatorIdentityPublicKey = System.Convert.ToBase64String(this.identityKeyPair.ExportSubjectPublicKeyInfo()),
                    InitiatorEphemeralPublicKey = System.Convert.ToBase64String(this.userPeers[index].ephemeralKeyPair.ExportSubjectPublicKeyInfo()),
                    MessageData = System.Convert.ToBase64String(encMessage),
                    Timestamp = DateTime.Now.ToString(),
                    SenderName = this.Name,
                    SenderId = this.id,
                };
            }

            stopwatch.Stop();
            Console.WriteLine("Elapsed Time for Encryption is {0} ms", stopwatch.ElapsedMilliseconds);
            //Add the ephemeral initiator public key to the blob and the initiator identity public key
            return msgModel;

            return null;
        }

        public string? DecryptMessageFromUser(MessageModel msgModel, int lastMessageSenderId) {
            //Get the encrypted message data
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            byte[] encryptedMessageData = System.Convert.FromBase64String(msgModel.MessageData);
            byte[] encText = new byte[encryptedMessageData.Length - 32];
            System.Buffer.BlockCopy(encryptedMessageData, 0, encText, 0, encryptedMessageData.Length - 32);
            //Get the ephemeral public key and identity public key
            byte[] identityPublicKey = System.Convert.FromBase64String(msgModel.InitatorIdentityPublicKey);
            byte[] ephemeralPublicKey = System.Convert.FromBase64String(msgModel.InitiatorEphemeralPublicKey);

            //Find the peer that sent the message
            bool found = false;
            int index = -1;
            for(int i =0; i < this.userPeers.Count; i++) {
                if (this.userPeers[i].Id == msgModel.SenderId) {
                    found = true;
                    index = i;
                    break;
                }
            }

            int bytesRead;
            var identityKey = Encryption.GenerateX25519Key();
            identityKey.ImportSubjectPublicKeyInfo(identityPublicKey, out bytesRead);
            var ephemeralKey = Encryption.GenerateX25519Key();
            ephemeralKey.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out bytesRead);

            var idKey = identityKey.PublicKey;
            var ephKey = ephemeralKey.PublicKey;
            //If the user was found it means the ephemeral key should be updated with the new one received
            byte[]? messageKey = null; 
            if(!found) {
                PeerModel pm = new PeerModel { Id = msgModel.SenderId, SenderName = this.Name, Name = msgModel.SenderName, identityKeyPair = this.identityKeyPair, signedPreKeyPair = this.signedPreKeyPair, oneTimeKeyPair = this.oneTimeKeyPair[0], peerIdentityPublicKey = idKey, peerEphemeralPublicKey = ephKey};
                pm.GenerateReceiverMasterSecret();
                this.userPeers.Add(pm);
                messageKey = pm.GenerateMessageKey();
            } else {
                this.userPeers[index].peerIdentityPublicKey = idKey;
                this.userPeers[index].peerEphemeralPublicKey = ephKey;
                //If last message sender id is different update the
                if(lastMessageSenderId != msgModel.SenderId)
                    this.userPeers[index].UpdateChainKeyAndRootKeyAfterChangingSender();
                messageKey = this.userPeers[index].GenerateMessageKey();
            }
#if DEBUG_MODE
            Console.WriteLine("Message key");
            foreach (var value in messageKey) {
                Console.Write(value.ToString("X2"));
            }
            Console.WriteLine();
            Console.WriteLine("Message key");
#endif

            string plainText = this.DecryptMessageBlob(encText, messageKey);
            stopwatch.Stop();
            Console.WriteLine("Elapsed Time for Decryption is {0} ms", stopwatch.ElapsedMilliseconds);
            return plainText;
        }
    }
}
