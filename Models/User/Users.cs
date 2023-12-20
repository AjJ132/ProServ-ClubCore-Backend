

using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class Users
    {
        [Key, MaxLength(450)]
        public string User_ID { get; set; }

        [MaxLength(50)]
        public string First_Name { get; set; }
        [MaxLength(50)]
        public string Last_Name { get; set; }

        public int User_Type { get; set; }

        public DateTime Date_Joined { get; set; }

        [MaxLength(450)]
        public string Club_ID { get; set; }

        public Users()
        {
        }
    }
}