using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_SarahahTelegrambot
{
    public class UserModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long LongSenderChatId { get; set; }
        public long? LongLastReceiverChatId { get; set; }
        [MaxLength(25)]
        public string StringSenderChatId { get; set; }
        [MaxLength(25)]
        public string? RegisterName { get; set; }
        public List<MessageInfoModel> MessagesInfo { get; set; }
    }
}
