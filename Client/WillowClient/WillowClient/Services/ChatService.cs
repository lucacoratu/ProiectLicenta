using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using WillowClient.Model;
using System.Net.Http.Json;
using System.Net;
using System.Net.Http.Headers;
using WillowClient.Database;
using System.Text.Json;
using System.Net.Security;

namespace WillowClient.Services
{
    public class ChatService
    {
        private ClientWebSocket client;
        private HttpClient m_httpClient;
        private HttpClientHandler m_handler;
        private CookieContainer m_CookieContainer;
        private List<Func<string, Task>> recvCallbacks;
        private DatabaseService databaseService;
        public ChatService(DatabaseService databaseService)
        {
            client = new ClientWebSocket();
            this.m_CookieContainer = new CookieContainer();
            this.m_handler = new HttpClientHandler();
            this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_httpClient = new HttpClient(this.m_handler);
            this.recvCallbacks = new();
            this.databaseService = databaseService;

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
            try {
                WebSocketReceiveResult result;
                string receivedMessage = "";
                var message = new ArraySegment<byte>(new byte[4096]);
                do {
                    result = await client.ReceiveAsync(message, CancellationToken.None);
                    if (result.MessageType != WebSocketMessageType.Text)
                        break;
                    var messageBytes = message.Skip(message.Offset).Take(result.Count).ToArray();
                    receivedMessage = Encoding.UTF8.GetString(messageBytes);
                    //Console.WriteLine("Received: {0}", receivedMessage);
                }
                while (!result.EndOfMessage);

                //Call all the callbackfunctions registered to be called when the read event finished
                foreach (var function in this.recvCallbacks) {
                    var res = function(receivedMessage);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                return;
            }
        }

        public async Task SendMessageAsync(string message)
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
            try {
                var url = Constants.chatServerUrl + "history/" + roomId.ToString();
                var response = await this.m_httpClient.GetAsync(url);
                //response = new HttpResponseMessage();
                return await response.Content.ReadFromJsonAsync<List<HistoryMessageModel>>();
            } catch (Exception ex) {
                return new List<HistoryMessageModel>();
            }
        }

        public async IAsyncEnumerable<HistoryMessageModel> GetMessageHistoryAsync(int roomId) {
            var url = Constants.chatServerUrl + "history/" + roomId.ToString();
            var response = await this.m_httpClient.GetAsync(url);
            //response = new HttpResponseMessage();
            var messages = await response.Content.ReadFromJsonAsync<List<HistoryMessageModel>>();
            if(messages != null) {
                foreach(var message in messages)
                    yield return message;
            }
        }

        public async IAsyncEnumerable<HistoryMessageModel> GetMessagesWithIdGreater(int roomId, int knownMessageId) {
            var url = Constants.chatServerUrl + "history/" + roomId.ToString() + "/" + knownMessageId.ToString();
            var response = await this.m_httpClient.GetAsync(url);
            var messages = await response.Content.ReadFromJsonAsync<List<HistoryMessageModel>>();
            foreach(var message in messages) 
                yield return message;
        }

        public async IAsyncEnumerable<HistoryMessageModel> GetMessageHistoryWithCache(int roomId) {
            var url = Constants.chatServerUrl + "history/" + roomId.ToString();
            //Get the local messages
            var localMessages = await this.databaseService.GetCachedEntry(url);
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
            };
            //Parse the messages
            if (localMessages != null) {
                List<HistoryMessageModel> messages = null;
                messages = JsonSerializer.Deserialize<List<HistoryMessageModel>>(localMessages, options);
                if (messages != null) {
                    //Return the most recent 20 messages from cache
                    for(int i = 0; i < messages.Count; i++) {
                        yield return messages[i];
                    }
                    //yield break;
                }
            }

            var response = await this.m_httpClient.GetAsync(url);
            var remoteMessages = await response.Content.ReadFromJsonAsync<List<HistoryMessageModel>>();
            if (localMessages == null && remoteMessages != null) {
                //Return the most recent 20 messages
                for(int i = 0; i < remoteMessages.Count; i++) {
                    yield return remoteMessages[i];
                }
            }
            _ = await this.databaseService.SaveKeyValueData(url, JsonSerializer.Serialize(remoteMessages));
        }

        public async Task<List<GroupModel>> GetGroups(int accountId, string session)
        {
            try {
                var url = Constants.serverURL + "/chat/groups/" + accountId.ToString();
                //Get the local groups from the sqlite3 database
                //var cachedGroups = await this.databaseService.GetCachedEntry(url);
                //if (cachedGroups != null || cachedGroups != "") {
                //    //Deserialize the cached data
                //    var localGroups = JsonSerializer.Deserialize<List<GroupModel>>(cachedGroups);
                //    //If there are any cached groups then return them
                //    if(localGroups.Count != 0)
                //        return localGroups;
                //}

                var baseAddress = new Uri(url);
                this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));

                var response = await this.m_httpClient.GetAsync(baseAddress);
                //var responseString = await response.Content.ReadAsStringAsync();
                //var remoteGroups =  JsonSerializer.Deserialize<List<GroupModel>>(responseString);
                ////Cache the remote groups
                //_ = await this.databaseService.SaveKeyValueData(url, responseString);
                return await response.Content.ReadFromJsonAsync<List<GroupModel>>();
            } catch(Exception ex) {
                Console.WriteLine(ex.Message);
                return new List<GroupModel>();
            }
        }

        public async IAsyncEnumerable<GroupModel> GetGroupsAsyncEnumerable(int accountId, string session) {
            var url = Constants.serverURL + "/chat/groups/" + accountId.ToString();
            var baseAddress = new Uri(url);
            this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));

            var response = await this.m_httpClient.GetAsync(baseAddress);
            var groups = await response.Content.ReadFromJsonAsync<List<GroupModel>>();
            groups = groups.OrderByDescending(group => group.LastMessageTimestamp).ToList();
            foreach( var group in groups)
                yield return group;
        }

        public async IAsyncEnumerable<GroupModel> GetGroupsWithCache(int accountId, string session) {
            var url = Constants.serverURL + "/chat/groups/" + accountId.ToString();

            //Load the cached data
            var cachedGroups = await this.databaseService.GetCachedEntry(url);
            List<GroupModel> localGroups = null;
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
            };
            if (cachedGroups != null) {
                //Deserialize the cached data
                localGroups = JsonSerializer.Deserialize<List<GroupModel>>(cachedGroups);
                //If there are any cached groups then return them
                if (localGroups != null) {
                    if (localGroups.Count != 0) {
                        //Sort the groups by the last message date
                        localGroups = localGroups.OrderByDescending(group => group.LastMessageTimestamp).ToList();
                        foreach (var group in localGroups) {
                            yield return group;
                        }
                        //yield break;
                    }
                }
            }

            List<GroupModel> remoteGroups = null;
            try {
                var baseAddress = new Uri(url);
                this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));
                //Get the remote groups if there is one new then return it
                var response = await this.m_httpClient.GetAsync(baseAddress);
                remoteGroups = await response.Content.ReadFromJsonAsync<List<GroupModel>>();
            } catch(Exception e) {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Got remote groups");

            //If the cached groups are null then return the remote groups
            if (localGroups == null || localGroups.Count == 0) {
                if (remoteGroups != null) {
                    //Sort the remote groups
                    remoteGroups = remoteGroups.OrderByDescending(group => group.LastMessageTimestamp).ToList();
                    foreach (var group in remoteGroups)
                        yield return group;
                }
            }

            //Cache the remote groups in the local database
            _ = await this.databaseService.SaveKeyValueData(url, JsonSerializer.Serialize(remoteGroups));
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

        public async Task<bool> UpdateGroupPicture(int roomId, Stream picture, string session) {
            using (var multipartFormContent = new MultipartFormDataContent()) {
                multipartFormContent.Add(new StringContent(roomId.ToString()), "roomId");

                var fileStreamContent = new StreamContent(picture);
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                multipartFormContent.Add(fileStreamContent, name: "groupPicture", fileName: "upload.png");

                //Add the session cookie
                var url = Constants.serverURL + "/chat/group/updatepicture";
                var baseAddress = new Uri(url);

                this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));

                var response = await this.m_httpClient.PostAsync(baseAddress, multipartFormContent);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            return false;
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
