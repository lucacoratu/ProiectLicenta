using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using WillowClient.Model;

namespace WillowClient.Services
{
    public class RegisterService
    {
        private HttpClient m_HttpClient;

        public RegisterService()
        {
            this.m_HttpClient = new HttpClient();
        }

        public async Task<string> RegisterAccount(RegisterModel model)
        {
            var url = "http://localhost:8080/accounts/register";
            var result = await this.m_HttpClient.PostAsync(url, JsonContent.Create(model));
            return await result.Content.ReadAsStringAsync();
        }
    }
}
