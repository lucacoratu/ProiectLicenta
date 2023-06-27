using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public class EncryptionMetricsModel {
        public string deviceInfo { get; set; }
	    public long elapsedMiliseconds { get; set; }
	    public int messageSize { get; set; }
    }
}
