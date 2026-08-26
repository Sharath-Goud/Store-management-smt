using Microsoft.EntityFrameworkCore;
using Store_Management.Models;

namespace Store_Management.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<User> Users { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Brand> BrandMaster { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.Username)
                    .IsUnique();
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.PartNumber)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ItemName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.BrandId)
                    .IsRequired();

                entity.Property(e => e.BrandName)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.ImagePath)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                entity.HasOne(e => e.Brand)
                    .WithMany()
                    .HasForeignKey(e => e.BrandId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ItemId)
                    .IsRequired();

                entity.Property(e => e.Quantity)
                    .IsRequired();

                entity.Property(e => e.Balance)
                    .IsRequired();

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.TransactionDate)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");


                entity.HasOne(e => e.Item)
                    .WithMany(e => e.Transactions)
                    .HasForeignKey(e => e.ItemId)
                    .OnDelete(DeleteBehavior.Cascade);

            });


            modelBuilder.Entity<Brand>(entity =>
            {
                entity.ToTable("BrandMaster");

                entity.HasKey(e => e.BrandId);


                entity.Property(e => e.BrandName)
                    .IsRequired()
                    .HasMaxLength(100);


                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);


                entity.Property(e => e.CreatedDate)
                    .HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<User>()
                .HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Password = "admin123",
                    Role = "Admin"
                });
        }
    }
}