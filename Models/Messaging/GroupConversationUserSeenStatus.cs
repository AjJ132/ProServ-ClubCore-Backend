using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProServ_ClubCore_Server_API.Models
{
    public class GroupConversationUserSeenStatus
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public Guid GroupConversation_ID { get; set; }

        [Required]
        public string User_ID { get; set; }

        [Required]
        public bool Seen { get; set; }
    }
}
