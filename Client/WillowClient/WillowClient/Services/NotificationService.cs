using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.LocalNotification;

namespace WillowClient.Services {
    public static class NotificationIdGenerator {
        private static int currentId = 0;
        public static int GetId() {
            currentId++;
            return currentId;
        }
    }

    public class NotificationService {
        public void SendPrivateChatNotification(string friendName, string messageReceived) {
#if ANDROID || IOS
            var request = new NotificationRequest {
                NotificationId = NotificationIdGenerator.GetId(),
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

        public void SendReactionNotification(string senderName, string emoji) {
#if ANDROID || IOS
            var request = new NotificationRequest {
                NotificationId = NotificationIdGenerator.GetId(),
                Title = senderName,
                Description = $"{senderName} reacted with {emoji}",
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
#if ANDROID || IOS
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
