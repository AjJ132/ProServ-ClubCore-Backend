using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class ConversationMessage
    {
        [Key]
        public Guid Message_ID { get; set; }
        [Required]
        public Guid Conversation_ID { get; set; }
        [Required, MaxLength(450)]
        public string Sender_ID { get; set; }
        [Required, MaxLength(250)]
        public string Message { get; set; }
        [Required]
        public DateTimeOffset Timestamp { get; set; }
    }
}
