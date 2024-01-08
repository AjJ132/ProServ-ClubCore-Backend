﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProServ_ClubCore_Server_API.Models
{
    public class ConversationUsers
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Required]
        public Guid Conversation_ID { get; set; }
        [Required, MaxLength(450)]
        public string User_ID { get; set; }
    }
}
