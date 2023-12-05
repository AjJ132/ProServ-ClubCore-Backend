

using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class Clubs
    {
        [Key, MaxLength(450)]
        public string Club_ID { get; set; }

        [Required, MaxLength(450)]
        public string Owner_ID { get; set; }

        [MaxLength(200)]
        public string Club_Name { get; set; }


        [MaxLength(50)]
        public string Club_Sport { get; set; }
        

        [MaxLength(40)]
        public string Club_City { get; set; }

        [MaxLength(2)]
        public string Club_State { get; set; }

        public int User_Count { get; set; }

        public DateTime Club_Date_Joined { get; set; }

       
        public Clubs()
        {
        }
    }
}