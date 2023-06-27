// See https://aka.ms/new-console-template for more information

using EndToEndWillow;
using System.Diagnostics;
using System.Text;

namespace EndToEndWIllow;
class Program {
    static Random rd = new Random();
    internal static string CreateString(int stringLength) {
        const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
        char[] chars = new char[stringLength];

        for (int i = 0; i < stringLength; i++) {
            chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
        }

        return new string(chars);
    }
    static void Main(string[] args) {
        //Create the users
        UserModel alice = new UserModel(1, "Alice");
        UserModel bob = new UserModel(2, "Bob");
        UserModel cindy = new UserModel(3, "Cindy");

        //Generate the keys for the users
        alice.GenerateNecessaryKeys();
        bob.GenerateNecessaryKeys();
        cindy.GenerateNecessaryKeys();

        for (int i = 0; i < args.Length; i++) {
            Console.WriteLine(args[i]);
        }

        //Create the peer models
        PeerModel alicePeer = new PeerModel { Id = alice.Id, Name = alice.Name, peerIdentityPublicKey = alice.IdentityPublicKey, peerSignedPrePublicKey = alice.SignedPrePublicKey, peerOneTimePublicKey = alice.OneTimePublicKey };
        PeerModel bobPeer = new PeerModel { Id = bob.Id, Name = bob.Name, peerIdentityPublicKey = bob.IdentityPublicKey, peerSignedPrePublicKey = bob.SignedPrePublicKey, peerOneTimePublicKey = bob.OneTimePublicKey};
        PeerModel cindyPeer = new PeerModel { Id = cindy.Id, Name = cindy.Name, peerIdentityPublicKey = cindy.IdentityPublicKey, peerSignedPrePublicKey = cindy.SignedPrePublicKey, peerOneTimePublicKey = cindy.OneTimePublicKey };

        int messageSize = int.Parse(args[0]);
        //Send a message from alice to bob
        Stopwatch stopwatch = Stopwatch.StartNew();
        string aliceMessage = CreateString(messageSize);
        var sentMessage = alice.EncryptMessageToUser(aliceMessage, bobPeer, 0);
        stopwatch.Stop();
        float encryptionTime = stopwatch.ElapsedMilliseconds;
        Console.WriteLine(encryptionTime);
        //string aliceMessage2 = "How are you?";
        //var sentMessage2 = alice.EncryptMessageToUser(aliceMessage2, bobPeer, sentMessage.SenderId);

        //Receive the message as bob
        string? res = bob.DecryptMessageFromUser(sentMessage, 0);
        Console.WriteLine(res);
        //string? res2 = bob.DecryptMessageFromUser(sentMessage2, sentMessage.SenderId);
        //Console.WriteLine(res2);

        ////Bob sends a message
        //string bobMessage = "Hello Alice!";
        //var bobSentMessage = bob.EncryptMessageToUser(bobMessage, alicePeer, sentMessage2.SenderId);

        //string bobMessage2 = "I am good, thanks for asking!";
        //var bobSentMessage2 = bob.EncryptMessageToUser(bobMessage2, alicePeer, bobSentMessage.SenderId);

        //string res3 = alice.DecryptMessageFromUser(bobSentMessage, sentMessage2.SenderId);
        //Console.WriteLine(res3);
        //string res4 = alice.DecryptMessageFromUser(bobSentMessage2, bobSentMessage.SenderId);
        //Console.WriteLine(res4);

        ////Send 10 messages from Alice to Bob
        //var bobEncryptedMessages = new List<MessageModel>();
        //int lastMessageSenderId = 0;

        //int start = 100;
        //int end = 90000;
        //int step = 100;
        //int numberMessages = (end - start) / step;
        //float[] encryptionTimes = new float[numberMessages];
        //int[] messageSizes = new int[numberMessages];
        //for(int i = start; i < end; i += step) {
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    //string aliceMessage = $"Message {i} from Alice!?";
        //    string aliceMessage = CreateString(i);
        //    //Console.WriteLine($"Alice sending message to Bob: {aliceMessage}");
        //    var encMsg = alice.EncryptMessageToUser(aliceMessage, bobPeer, lastMessageSenderId);
        //    bobEncryptedMessages.Add(encMsg);
        //    //Console.WriteLine($"Encrypted message = {encMsg.MessageData}");
        //    lastMessageSenderId = bobEncryptedMessages[bobEncryptedMessages.Count - 1].SenderId;
        //    stopwatch.Stop();
        //    float encryptionTime = stopwatch.ElapsedMilliseconds;
        //    encryptionTimes[i/step - 1] = encryptionTime;
        //    messageSizes[i / step - 1] = i;
        //}

        //float[] decryptionTimes = new float[numberMessages];
        ////Decrypt all messages from alice (as bob)
        //lastMessageSenderId = 0;
        //var bobDecryptedMessages = new List<string>();
        //for(int i = 0; i < bobEncryptedMessages.Count; i++) {
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    var res = bob.DecryptMessageFromUser(bobEncryptedMessages[i], lastMessageSenderId);
        //    //Console.WriteLine($"Bob received message from Alice: {res}");
        //    bobDecryptedMessages.Add(res);
        //    lastMessageSenderId = bobEncryptedMessages[i].SenderId;
        //    stopwatch.Stop();
        //    float decryptionTime = stopwatch.ElapsedMilliseconds;
        //    decryptionTimes[i] = decryptionTime;
        //}

        //string[] enc_times = new string[numberMessages];
        //string[] dec_times = new string[numberMessages];
        //string[] message_sizes = new string[numberMessages];
        //for (int i = 0; i < encryptionTimes.Length; i++) {
        //    enc_times[i] = encryptionTimes[i].ToString();
        //}
        //for (int i = 0; i < decryptionTimes.Length; i++) {
        //    dec_times[i] = decryptionTimes[i].ToString();
        //}
        //for (int i = 0; i < messageSizes.Length; i++) {
        //    message_sizes[i] = messageSizes[i].ToString();
        //}

        //File.WriteAllLines("./encryption_times.txt", enc_times);
        //File.WriteAllLines("./decryption_times.txt", dec_times);
        //File.WriteAllLines("./message_sizes.txt", message_sizes);


        ////Send 5 messages from Bob to Alice
        //var aliceEncryptedMessages = new List<MessageModel>();
        //for(int i = 0; i < 2; i++) {
        //    string bobMessage = $"Bob response {i} ?????!!!!";
        //    Console.WriteLine($"Bob sending message to Alice: {bobMessage}");
        //    var encMsg = bob.EncryptMessageToUser(bobMessage, alicePeer, lastMessageSenderId);
        //    Console.WriteLine($"Encrypted message = {encMsg.MessageData}");
        //    aliceEncryptedMessages.Add(encMsg);
        //    lastMessageSenderId = aliceEncryptedMessages[aliceEncryptedMessages.Count - 1].SenderId;
        //}

        ////Decrypt all messages from bob
        //lastMessageSenderId = bobEncryptedMessages[bobEncryptedMessages.Count - 1].SenderId;
        //var aliceDecryptedMessages = new List<string>();
        //foreach (var message in aliceEncryptedMessages) {
        //    var res = alice.DecryptMessageFromUser(message, lastMessageSenderId);
        //    Console.WriteLine($"Alice received message from Bob: {res}");
        //    aliceDecryptedMessages.Add(res);
        //    lastMessageSenderId = message.SenderId;
        //}

        ////Alice sends 3 messages to Cindy
        ////lastMessageSenderId = 0;
        //lastMessageSenderId = 0;
        //var cindyEncryptedMessages = new List<MessageModel>();
        //for (int i = 0; i < 3; i++) {
        //    string aliceMessage = $"Message {i} from Alice to Cindy!?";
        //    Console.WriteLine($"Alice sending message to Cindy: {aliceMessage}");
        //    cindyEncryptedMessages.Add(alice.EncryptMessageToUser(aliceMessage, cindyPeer, lastMessageSenderId));
        //    lastMessageSenderId = cindyEncryptedMessages[cindyEncryptedMessages.Count - 1].SenderId;
        //}

        ////Decrypt messages from Alice (as Cindy)
        //lastMessageSenderId = 0;
        //var cindyDecryptedMessages = new List<string>();
        //foreach (var message in cindyEncryptedMessages) {
        //    var res = cindy.DecryptMessageFromUser(message, lastMessageSenderId);
        //    Console.WriteLine($"Cindy received message from Alice: {res}");
        //    cindyDecryptedMessages.Add(res);
        //    lastMessageSenderId = message.SenderId;
        //}

        //Bob now wants to send a message
    }
}
