using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Models;
using Microsoft.AspNetCore.Identity;

namespace ProServ_ClubCore_Server_API.Database
{
    public class ProServDbContext : IdentityDbContext
    {
        public ProServDbContext(DbContextOptions<ProServDbContext> options)
            : base(options)
        {
        }

        //User dbsets
        public virtual DbSet<Users> Users { get; set; }
        //Clubs dbsets
        public virtual DbSet<Teams> Teams { get; set; }
        //Events dbsets
        public virtual DbSet<Calendar_Event> CalendarEvents { get; set; }

        //Utilities dbsets
        //public virtual DbSet<UserTeamJunction> UserTeamJunctions { get; set; }

        //Messaging dbsets
        public virtual DbSet<DirectConversation> DirectConversations { get; set; }
        public virtual DbSet<DirectMessage> DirectMessages { get; set; }
        public virtual DbSet<GroupConversation> GroupConversations { get; set; }
        public virtual DbSet<ConversationUsers> ConversationUsers { get; set; }
        public virtual DbSet<ConversationMessage> ConversationMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

        }

    }
}