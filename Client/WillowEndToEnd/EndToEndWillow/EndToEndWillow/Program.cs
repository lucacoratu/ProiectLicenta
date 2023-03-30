// See https://aka.ms/new-console-template for more information

using EndToEndWillow;
using System.Text;

namespace EndToEndWIllow;
class Program {
    static void Main(string[] args) {
        //Create the users
        UserModel alice = new UserModel(1, "Alice");
        UserModel bob = new UserModel(2, "Bob");
        UserModel cindy = new UserModel(3, "Cindy");

        //Generate the keys for the users
        alice.GenerateNecessaryKeys();
        bob.GenerateNecessaryKeys();
        cindy.GenerateNecessaryKeys();

        //Create the peer models
        PeerModel alicePeer = new PeerModel { Id = alice.Id, Name = alice.Name, peerIdentityPublicKey = alice.IdentityPublicKey, peerSignedPrePublicKey = alice.SignedPrePublicKey, peerOneTimePublicKey = alice.OneTimePublicKey };
        PeerModel bobPeer = new PeerModel { Id = bob.Id, Name = bob.Name, peerIdentityPublicKey = bob.IdentityPublicKey, peerSignedPrePublicKey = bob.SignedPrePublicKey, peerOneTimePublicKey = bob.OneTimePublicKey};
        PeerModel cindyPeer = new PeerModel { Id = cindy.Id, Name = cindy.Name, peerIdentityPublicKey = cindy.IdentityPublicKey, peerSignedPrePublicKey = cindy.SignedPrePublicKey, peerOneTimePublicKey = cindy.OneTimePublicKey };

        ////Send a message from alice to bob
        //string aliceMessage = "Hello Bob!";
        //var sentMessage = alice.EncryptMessageToUser(aliceMessage, bobPeer, 0);
        //string aliceMessage2 = "How are you?";
        //var sentMessage2 = alice.EncryptMessageToUser(aliceMessage2, bobPeer, sentMessage.SenderId);

        ////Receive the message as bob
        //string? res = bob.DecryptMessageFromUser(sentMessage, 0);
        //Console.WriteLine(res);
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

        //Send 10 messages from Alice to Bob
        var bobEncryptedMessages = new List<MessageModel>();
        int lastMessageSenderId = 0;
        for(int i = 0; i < 3; i++) {
            string aliceMessage = $"Message {i} from Alice!?";
            Console.WriteLine($"Alice sending message to Bob: {aliceMessage}");
            var encMsg = alice.EncryptMessageToUser(aliceMessage, bobPeer, lastMessageSenderId);
            bobEncryptedMessages.Add(encMsg);
            Console.WriteLine($"Encrypted message = {encMsg.MessageData}");
            lastMessageSenderId = bobEncryptedMessages[bobEncryptedMessages.Count - 1].SenderId;
        }

        //Decrypt all messages from alice (as bob)
        lastMessageSenderId = 0;
        var bobDecryptedMessages = new List<string>();
        foreach(var message in bobEncryptedMessages) {
            var res = bob.DecryptMessageFromUser(message, lastMessageSenderId);
            Console.WriteLine($"Bob received message from Alice: {res}");
            bobDecryptedMessages.Add(res);
            lastMessageSenderId = message.SenderId;
        }

        //Send 5 messages from Bob to Alice
        var aliceEncryptedMessages = new List<MessageModel>();
        for(int i =0; i < 2; i++) {
            string bobMessage = $"Bob response {i} ?????!!!!";
            Console.WriteLine($"Bob sending message to Alice: {bobMessage}");
            var encMsg = bob.EncryptMessageToUser(bobMessage, alicePeer, lastMessageSenderId);
            Console.WriteLine($"Encrypted message = {encMsg.MessageData}");
            aliceEncryptedMessages.Add(encMsg);
            lastMessageSenderId = aliceEncryptedMessages[aliceEncryptedMessages.Count - 1].SenderId;
        }

        //Decrypt all messages from bob
        lastMessageSenderId = bobEncryptedMessages[bobEncryptedMessages.Count - 1].SenderId;
        var aliceDecryptedMessages = new List<string>();
        foreach (var message in aliceEncryptedMessages) {
            var res = alice.DecryptMessageFromUser(message, lastMessageSenderId);
            Console.WriteLine($"Alice received message from Bob: {res}");
            aliceDecryptedMessages.Add(res);
            lastMessageSenderId = message.SenderId;
        }

        //Alice sends 3 messages to Cindy
        //lastMessageSenderId = 0;
        lastMessageSenderId = 0;
        var cindyEncryptedMessages = new List<MessageModel>();
        for (int i = 0; i < 3; i++) {
            string aliceMessage = $"Message {i} from Alice to Cindy!?";
            Console.WriteLine($"Alice sending message to Cindy: {aliceMessage}");
            cindyEncryptedMessages.Add(alice.EncryptMessageToUser(aliceMessage, cindyPeer, lastMessageSenderId));
            lastMessageSenderId = cindyEncryptedMessages[cindyEncryptedMessages.Count - 1].SenderId;
        }

        //Decrypt messages from Alice (as Cindy)
        lastMessageSenderId = 0;
        var cindyDecryptedMessages = new List<string>();
        foreach (var message in cindyEncryptedMessages) {
            var res = cindy.DecryptMessageFromUser(message, lastMessageSenderId);
            Console.WriteLine($"Cindy received message from Alice: {res}");
            cindyDecryptedMessages.Add(res);
            lastMessageSenderId = message.SenderId;
        }

        //Bob now wants to send a message
    }
}
