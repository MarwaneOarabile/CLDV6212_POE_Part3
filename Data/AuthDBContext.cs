using ABCRetailers_ST10436124.Models;
using Microsoft.EntityFrameworkCore;


namespace ABCRetailers_ST10436124.Data
{

    public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();

                entity.Property(u => u.Role)
                    .HasDefaultValue("Customer");

                entity.Property(u => u.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(u => u.LastLogin)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // Cart configuration
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasOne(c => c.User)
                    .WithMany(u => u.Carts)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(c => c.Status)
                    .HasDefaultValue("Active");

                entity.Property(c => c.CreatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(c => c.UpdatedDate)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // CartItem configuration
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ci => ci.AddedDate)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(ci => ci.Quantity)
                    .HasDefaultValue(1);

                // Index for better performance when querying cart items
                entity.HasIndex(ci => ci.CartId);
                entity.HasIndex(ci => ci.ProductId);
            });
        }
    }
}
