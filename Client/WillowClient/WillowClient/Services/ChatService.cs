using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using WillowClient.Model;
using System.Net.Http.Json;

namespace WillowClient.Services
{
    public class ChatService
    {
        private ClientWebSocket client;
        private HttpClient httpClient;
        private List<Func<string, int>> recvCallbacks;
        public ChatService()
        {
            client = new ClientWebSocket();
            this.httpClient = new HttpClient();
            this.recvCallbacks = new();
            this.ConnectToServerAsync();
        }

        public async void ConnectToServerAsync()
        {
            if (client.State == WebSocketState.Open)
                return;

            await client.ConnectAsync(new Uri("ws://localhost:8087/ws"), CancellationToken.None);

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
            foreach(var function in this.recvCallbacks)
            {
                var res = function(receivedMessage);
            }
        }

        public async void SendMessageAsync(string message)
        {
            //if (!CanSendMessage(message))
            //    return;

            var byteMessage = Encoding.UTF8.GetBytes(message);
            var segmnet = new ArraySegment<byte>(byteMessage);

            await client.SendAsync(segmnet, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task<string> GetRoomId(GetRoomIdModel getRoomModel)
        {
            var response = await this.httpClient.PostAsync("http://localhost:8087/privateroom", JsonContent.Create(getRoomModel));
            //Parse the result from the chat service to get the room id
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<HistoryMessageModel>> GetMessageHistory(int roomId)
        {
            var response = await this.httpClient.GetAsync("http://localhost:8087/history/" + roomId.ToString());
            return await response.Content.ReadFromJsonAsync<List<HistoryMessageModel>>();
        }

        public void RegisterReadCallback(Func<string, int> callbackFunction)
        {
            this.recvCallbacks.Add(callbackFunction);
        }
    }
}
