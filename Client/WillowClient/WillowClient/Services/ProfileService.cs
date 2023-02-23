using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WillowClient.Model;

namespace WillowClient.Services {
    public class ProfileService {

        private HttpClientHandler m_handler;
        private HttpClient m_httpClient;
        private CookieContainer m_CookieContainer;
        public ProfileService() {
            this.m_CookieContainer = new CookieContainer();
            this.m_handler = new HttpClientHandler();
            this.m_handler.CookieContainer = this.m_CookieContainer;
            this.m_httpClient = new HttpClient(this.m_handler);
        }

        public async Task<bool> ChangeProfilePicture(Stream photoStream, int accountId, string session) {
            using (var multipartFormContent = new MultipartFormDataContent()) {
                multipartFormContent.Add(new StringContent(accountId.ToString()), "accountId");

                var fileStreamContent = new StreamContent(photoStream);
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                multipartFormContent.Add(fileStreamContent, name: "profile", fileName: "upload.png");

                //Add the session cookie
                var url = Constants.serverURL + "/accounts/picture";
                var baseAddress = new Uri(url);

                this.m_CookieContainer.Add(baseAddress, new Cookie("session", session));

                var response = await this.m_httpClient.PostAsync(baseAddress, multipartFormContent);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            return false;
        }
    }
}
