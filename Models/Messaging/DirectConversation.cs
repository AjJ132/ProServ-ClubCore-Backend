using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class DirectConversation
    {
        [Key]
        public Guid Conversation_ID { get; set; }
        [Required, MaxLength(450)]
        public string User1_ID { get; set; }
        [Required, MaxLength(450)]
        public string User2_ID { get; set; }
        [Required]
        public DateTimeOffset Date_Created { get; set; }
    }
}
