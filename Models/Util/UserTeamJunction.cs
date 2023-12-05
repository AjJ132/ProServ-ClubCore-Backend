

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProServ_ClubCore_Server_API.Models
{
    public class UserTeamJunction
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EntryID { get; set; }

        [MaxLength(450)]
        public string User_ID { get; set; }

        [MaxLength(450)]
        public string Team_ID { get; set; }

        public int permission_level { get; set; }

       
        public UserTeamJunction()
        {
        }
    }
}