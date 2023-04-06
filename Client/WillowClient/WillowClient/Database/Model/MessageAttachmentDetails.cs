using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WillowClient.Database.Model {
    [Table("message_attachment_details")]
    public class MessageAttachmentDetails {
        [Column("messageId")]
        public int MessageId { get; set; }
        [Column("blobUuid"), PrimaryKey]
        public string BlobUuid { get; set; }
        [Column("downloaded")]
        public bool Downloaded { get; set; }
        [Column("localFilepath")]
        public string LocalFilepath { get; set; }
        [Column("attachmentSize")]
        public long AttachmentSize { get; set; }
    }
}
