using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model
{
    public class HistoryReactionModel
    {
		public int MessageId { get; set; }
	    public int ReactionId { get; set; }
		public int SenderId { get; set; }
		public string Emoji { get; set; }
		public string ReactionDate { get; set; }
	}
}
