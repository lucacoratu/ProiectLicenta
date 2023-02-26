using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using WillowClient.Model;
using System.Net.Http.Json;
using System.Net;

namespace WillowClient.Services
{
    public class ChatService
    {
        private ClientWebSocket client;
        private HttpClient m_httpClient;
        private HttpClientHandler m_handler;
        private CookieContainer m_CookieContainer;
        private List<Func<string, Task>> recvCallbacks;
        public ChatService()
        {
            client = new ClientWebSocket();
            this.m_CookieContainer = new CookieContainer();
            this.m_handler = new HttpClientHandler();
            this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_httpClient = new HttpClient(this.m_handler);
            this.recvCallbacks = new();
            this.ConnectToServerAsync();
        }

        public async void ConnectToServerAsync()
        {
            if (client.State == WebSocketState.Open)
                return;

            await client.ConnectAsync(new Uri(Constants.wsServerUrl + "ws"), CancellationToken.None);

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
            var response = await this.m_httpClient.PostAsync(Constants.chatServerUrl + "privateroom", JsonContent.Create(getRoomModel));
            //Parse the result from the chat service to get the room id
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<HistoryMessageModel>> GetMessageHistory(int roomId)
        {
            var response = await this.m_httpClient.GetAsync(Constants.chatServerUrl + "history/" + roomId.ToString());
            return await response.Content.ReadFromJsonAsync<List<HistoryMessageModel>>();
        }

        public async Task<List<GroupModel>> GetGroups(int accountId, string session)
        {
            var url = Constants.serverURL + "/chat/groups/" + accountId.ToString();
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));

            var response = await this.m_httpClient.GetAsync(baseAddress);
            return await response.Content.ReadFromJsonAsync<List<GroupModel>>();
        }

        public async Task<List<CommonGroupModel>> GetCommonGroups(int idFirst, int idSecond, string session) {
            var url = Constants.serverURL + "/chat/commongroups/" + idFirst.ToString() + "/" + idSecond.ToString();
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));

            var response = await this.m_httpClient.GetAsync(baseAddress);
            List<CommonGroupModel> commonGroups = new();
            if (response.IsSuccessStatusCode) {
                commonGroups = await response.Content.ReadFromJsonAsync<List<CommonGroupModel>>();
            }

            return commonGroups;
        }

        public void RegisterReadCallback(Func<string, Task> callbackFunction)
        {
            this.recvCallbacks.Add(callbackFunction);
        }

        public void UnregisterReadCallback(Func<string, Task> callbackFunction)
        {
            this.recvCallbacks.Remove(callbackFunction);
        }

        public void PopReadCallback()
        {
            this.recvCallbacks.RemoveAt(this.recvCallbacks.Count - 1);
        }
    }
}
