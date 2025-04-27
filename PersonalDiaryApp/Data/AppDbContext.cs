using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalDiaryApp.Entities;

namespace PersonalDiaryApp.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Diary> Diaries { get; set; }
        public DbSet<DiaryPhoto> DiaryPhotos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Diary>()
                .HasMany(d => d.Photos)
                .WithOne(p => p.Diary)
                .HasForeignKey(p => p.DiaryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
