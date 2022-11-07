using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class CreateGroupMessageModel
    {
        public int creatorID { get; set; }
        public string groupName { get; set; }
        public List<int> participants { get; set; } 
    }
}
