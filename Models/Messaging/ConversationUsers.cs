using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class ConversationUsers
    {
        [Key]
        public Guid Conversation_ID { get; set; }
        [Required, MaxLength(450)]
        public string User_ID { get; set; }
    }
}
