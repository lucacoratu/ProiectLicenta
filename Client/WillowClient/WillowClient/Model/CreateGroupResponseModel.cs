using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class CreateGroupResponseModel
    {
        public int RoomId { get; set; }
        public int CreatorId { get; set; }
        public string GroupName { get; set; }
        public IList<int> Participants { get; set; }
    }
}
