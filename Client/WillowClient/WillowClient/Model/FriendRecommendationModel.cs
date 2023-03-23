using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public class FriendRecommendationModel {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string JoinDate { get; set; }
        public string Status { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string About { get; set; }
    }

}
