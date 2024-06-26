using Microsoft.EntityFrameworkCore;

namespace my_new_app.Model
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> MyUsers { get; set; }
        public DbSet<MyImages> MyImages { get; set; }
        public DbSet<Segment> Segments { get; set; }
        //public DbSet<RefreshToken> RefreshTokens { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseSerialColumns();
            modelBuilder.Entity<User>()
                .Property(b => b.RefreshTokenExpiryTime)
                .HasColumnType("local");

            modelBuilder.Entity<MyImages>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.ApplyConfiguration(new MyImagesConfiguration());
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await MyUsers.SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task UpdateUserAsync(User user)
        {
            MyUsers.Update(user);
            await SaveChangesAsync();
        }
    }
}