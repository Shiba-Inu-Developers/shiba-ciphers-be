using Microsoft.EntityFrameworkCore;

namespace my_new_app.Model
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> MyUsers { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSerialColumns();
            modelBuilder.Entity<User>()
                .Property(b => b.RefreshTokenExpiryTime)
                .HasColumnType("local");
        }
    }
}