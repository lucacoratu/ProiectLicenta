using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.LocalNotification;

namespace WillowClient.Services {
    public class NotificationService {
        public void SendPrivateChatNotification(string friendName, string messageReceived) {
#if ANDROID
            var request = new NotificationRequest {
                NotificationId = 1337,
                Title = friendName,
                Description = messageReceived,
                BadgeNumber = 42,
                Schedule = new NotificationRequestSchedule {
                    NotifyTime = DateTime.Now.AddSeconds(1),
                    NotifyAutoCancelTime = DateTime.Now.AddSeconds(3)
                }
            };

            LocalNotificationCenter.Current.Show(request);
#endif
        }

        public void SendGroupChatNotification(string groupName, string messageReceived) {
#if ANDROID
            var request = new NotificationRequest {
                NotificationId = 13337,
                Title = groupName,
                Description = messageReceived,
                BadgeNumber = 42,
                Schedule = new NotificationRequestSchedule {
                    NotifyTime = DateTime.Now.AddSeconds(1),
                    NotifyAutoCancelTime = DateTime.Now.AddSeconds(3)
                }
            };

            LocalNotificationCenter.Current.Show(request);
#endif
        }
    }
}
