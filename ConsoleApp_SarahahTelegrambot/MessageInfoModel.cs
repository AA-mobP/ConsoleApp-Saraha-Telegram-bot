using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_SarahahTelegrambot
{
    public class MessageInfoModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SenderMessageId { get; set; }
        [Required]
        public int ReceiverMessageId { get; set; }
        public Nullable<int> ReplyToMessageId { get; set; }
        [Required]
        public long ReceiverChatId { get; set; }
        [Required]
        public long SenderChatId { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime SendDate { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime ReadDate { get; set; }
        public string TextMessage { get; set; }
        
        [ForeignKey("SenderChatId")]
        public UserModel User { get; set; }
    }
}
