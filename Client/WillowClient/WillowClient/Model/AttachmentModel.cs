using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Model {
    public class AttachmentModel {
        //The hash of the blob data (base64 encoded)
        public string HashBase64 { get; set; }
        //The hmac key (base64 encoded)
        public string HMACKeyBase64 { get; set; }
        //The AES key (base64 encoded)
        public string AesKeyBase64 { get; set; }
        //The AES IV (base64 encoded)
        public string AesIvBase64 { get; set; }
        //The blob location
        public string BlobUuid { get; set; }
        //The attachment type
        public string AttachmentType { get; set; }
        //The filename of the attachment if it is not photo or video
        public string Filename { get; set; }
        //Caption for the attachment if is set by the user
        public string Caption { get; set; }
    }
}
