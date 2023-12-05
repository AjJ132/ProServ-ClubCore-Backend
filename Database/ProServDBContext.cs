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
        public virtual DbSet<Coaches> Coaches { get; set; }
        public virtual DbSet<Athletes> Athletes { get; set; }

        //Clubs dbsets
        public virtual DbSet<Clubs> Clubs { get; set; }

        //Utilities dbsets
        //public virtual DbSet<UserTeamJunction> UserTeamJunctions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //User to athlete relationship
            modelBuilder.Entity<Users>()
                .HasOne<Athletes>(s => s.Athlete)
                .WithOne(ad => ad.User)
                .HasForeignKey<Athletes>(ad => ad.User_ID)
                .IsRequired(false);

            modelBuilder.Entity<Athletes>()
                .HasOne<Users>(s => s.User)
                .WithOne(ad => ad.Athlete)
                .HasForeignKey<Users>(ad => ad.User_ID)
                .IsRequired(false);

            //User to coach relationship
            modelBuilder.Entity<Users>()
                .HasOne<Coaches>(s => s.Coach)
                .WithOne(ad => ad.User)
                .HasForeignKey<Coaches>(ad => ad.User_ID)
                .IsRequired(false);

            modelBuilder.Entity<Coaches>()
                .HasOne<Users>(s => s.User)
                .WithOne(ad => ad.Coach)
                .HasForeignKey<Users>(ad => ad.User_ID)
                .IsRequired(false);

            base.OnModelCreating(modelBuilder);

        }

    }
}