using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProServ_ClubCore_Server_API.Models
{
    public class Calendar_Event
    {
        [Key]
        public Guid Event_ID { get; set; }
        [Required, MaxLength(50)]
        public string Title { get; set; }
        [MaxLength(250)]
        public string Description { get; set; }
        [Required]
        public DateTimeOffset StartDate { get; set; }
        [Required]
        public DateTimeOffset EndDate { get; set; }
        [Required, MaxLength(10)]
        public string Color { get; set; } //Will be in the form of a hex. Can be 6 or 8 characters long.8 For the alpha channel.
        [Required, MaxLength(450)]
        public string User_ID { get; set; } //450 char user id from identity user management
        [Required, MaxLength(450)]
        public string Creator_ID { get; set; } //450 char user id from identity user management 
        [Required]
        public DateTimeOffset Date_Created { get; set; }

        public Calendar_Event()
        {
            //ensure start date is before end date
            if (StartDate > EndDate)
            {
                throw new Exception("Start date cannot be after end date");
            }

            Event_ID = Guid.NewGuid();
        }
    }
}
