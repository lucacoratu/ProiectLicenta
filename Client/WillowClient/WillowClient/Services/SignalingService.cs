using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using WillowClient.Model;

namespace WillowClient.Services
{
    public class SignalingService
    {
        private ClientWebSocket client;
        private HttpClient httpClient;
        private List<Func<string, Task>> recvCallbacks;
        public SignalingService()
        {
            client = new ClientWebSocket();
            client.Options.RemoteCertificateValidationCallback += (o, c, ch, er) => true;
            this.httpClient = new HttpClient();
            this.recvCallbacks = new();
            //this.ConnectToServerAsync();
        }

        public async void ConnectToServerAsync()
        {
            if (client.State == WebSocketState.Open)
                return;

            await client.ConnectAsync(new Uri(Constants.signalingWsUrl), CancellationToken.None);

            await Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await ReadMessage();
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        async Task ReadMessage()
        {
            WebSocketReceiveResult result;
            string receivedMessage = "";
            var message = new ArraySegment<byte>(new byte[4096]);
            do
            {
                result = await client.ReceiveAsync(message, CancellationToken.None);
                if (result.MessageType != WebSocketMessageType.Text)
                    break;
                var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                receivedMessage = Encoding.UTF8.GetString(messageBytes);
                //Console.WriteLine("Received: {0}", receivedMessage);
            }
            while (!result.EndOfMessage);

            //Call all the callbackfunctions registered to be called when the read event finished
            foreach (var function in this.recvCallbacks)
            {
                var res = function(receivedMessage);
            }
        }

        public void RegisterReadCallback(Func<string, Task> callbackFunction)
        {
            this.recvCallbacks.Add(callbackFunction);
        }
    }
}
