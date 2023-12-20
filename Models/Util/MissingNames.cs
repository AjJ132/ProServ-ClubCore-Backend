using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace ProServ_ClubCore_Server_API.Models.Util
{
    public class MissingNames
    {
        [Required, MaxLength(50)]
        public string Email { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [MaxLength(6)]
        public string TeamCode { get; set; }
    }
}
