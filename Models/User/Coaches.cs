

using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models
{
    public class Coaches
    {
        [Key, MaxLength(450)]
        public string User_ID { get; set; }

        [MaxLength(450)]
        public string Club_ID { get; set; }

        public int Coach_Type { get; set; }

        public bool Allow_Multiple_Teams { get; set; }

        public virtual Users User { get; set; }
        public Coaches()
        {
        }
    }
}