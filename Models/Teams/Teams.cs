

using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class Teams
    {
        [Key, MaxLength(450)]
        public string Team_ID { get; set; }

        [Required, MaxLength(450)]
        public string Owner_ID { get; set; }

        [MaxLength(200)]
        public string Team_Name { get; set; }


        [MaxLength(50)]
        public string Team_Sport { get; set; }
        

        [MaxLength(40)]
        public string Team_City { get; set; }

        [MaxLength(2)]
        public string Team_State { get; set; }

        [MaxLength(6)]
        public string Team_Join_Code { get; set; }

        public int User_Count { get; set; }

        public DateTime Team_Date_Joined { get; set; }

       
        public Teams()
        {
        }
    }
}