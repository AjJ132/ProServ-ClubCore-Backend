using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class GroupConversation
    {
        [Key]
        public Guid Conversation_ID { get; set; }

        [Required, MaxLength(450)]
        public string Creator_ID { get; set; } //450 char user id from identity user management

        [Required, MaxLength(50)]
        public string Title { get; set; }

        [Required]
        public DateTimeOffset Date_Created { get; set; }

        [Required]
        public int Group_Type { get; set; } //types will be Conversation 1, Message Blast 2, and potentially more in the future


    }
}
