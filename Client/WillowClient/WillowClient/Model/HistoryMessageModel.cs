using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class HistoryMessageModel
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public string Data { get; set; }
        public string SendDate { get; set; }
        public int UserId { get; set; }
        public List<ReactionModel> Reactions { get; set; }
    }
}
