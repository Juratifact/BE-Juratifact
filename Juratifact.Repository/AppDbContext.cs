using Juratifact.Repository.Entity;
using Microsoft.EntityFrameworkCore;

namespace Juratifact.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartDetail> CartDetails { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<IdentityDocument> IdentityDocuments { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductMedia> ProductMedia { get; set; }
    public DbSet<ProductPromotion> ProductPromotions { get; set; }
    public DbSet<PromotionPackage> PromotionPackages { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<SellerOrder> SellerOrders { get; set; }
    public DbSet<SellerReview> SellerReviews { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<ProductComment> ProductComments { get; set; }
    public DbSet<UserPromotionSubscription> UserPromotionSubscriptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Dispute>  Disputes { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ==================== USER ====================
        modelBuilder.Entity<User>(builder =>
        {
            modelBuilder.Entity<ProductComment>()
                .HasOne(pc => pc.Parent)
                .WithMany(pc => pc.Children)
                .HasForeignKey(pc => pc.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(u => u.HashedPassword)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(u => u.UserName)
                .HasMaxLength(100);

            builder.Property(u => u.ProfilePicture)
                .HasMaxLength(500);
            
            builder.Property(u => u.Address)
                .HasMaxLength(500);

            // TrustScore = TotalTrustScore / SellerReviewAmount, giá trị từ 0.00 đến 5.00
            builder.Property(u => u.TrustScore)
                .HasPrecision(3, 2);

            // TotalTrustScore = tổng điểm sao tích lũy (mỗi lần đánh giá +1 đến +5)
            builder.Property(u => u.TotalTrustScore)
                .HasPrecision(10, 2);

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_User_TrustScore_Range",
                "\"TrustScore\" >= 0 AND \"TrustScore\" <= 5"));

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique();

        });
        
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.UserPromotionSubscription)
            .WithOne(s => s.Transaction)
            .HasForeignKey<UserPromotionSubscription>(s => s.TransactionId);

        modelBuilder.Entity<SellerOrder>(builder =>
        {
            builder.HasOne(so => so.Order)
                .WithMany(o => o.SellerOrders)
                .HasForeignKey(so => so.OrderId);

            builder.HasOne(so => so.Seller)
                .WithMany()
                .HasForeignKey(so => so.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(so => so.Shipper)
                .WithMany()
                .HasForeignKey(so => so.ShipperId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(so => so.Code)
                .IsUnique();
        });

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.SellerOrder)
            .WithMany(so => so.OrderDetails)
            .HasForeignKey(od => od.SellerOrderId);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.SellerOrder)
            .WithMany(so => so.Transactions)
            .HasForeignKey(t => t.SellerOrderId);

        modelBuilder.Entity<Dispute>()
            .HasOne(d => d.SellerOrder)
            .WithMany(so => so.Disputes)
            .HasForeignKey(d => d.SellerOrderId);

        modelBuilder.Entity<SellerReview>()
            .HasOne(sr => sr.SellerOrder)
            .WithMany(so => so.SellerReviews)
            .HasForeignKey(sr => sr.SellerOrderId);
        
        // 1. Khiếu nại của người mua
        modelBuilder.Entity<Dispute>()
            .HasOne(d => d.Buyer)
            .WithMany(u => u.BuyerDisputes) // Đã thêm: trỏ về danh sách BuyerDisputes trong User
            .HasForeignKey(d => d.BuyerId)
            .OnDelete(DeleteBehavior.Restrict); 

        // 2. Khiếu nại do Admin xử lý
        modelBuilder.Entity<Dispute>()
            .HasOne(d => d.ResolvedByAdmin)
            .WithMany(u => u.ResolvedDisputes) // Đã thêm: trỏ về danh sách ResolvedDisputes trong User
            .HasForeignKey(d => d.ResolvedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==================== SEED DATA ====================
        SeedData(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var buyerRoleId = new Guid("00000000-0000-0000-0000-000000000001");
        var sellerRoleId = new Guid("00000000-0000-0000-0000-000000000002");
        var adminRoleId = new Guid("00000000-0000-0000-0000-000000000003");
        var shipperRoleId = new Guid("00000000-0000-0000-0000-000000000004");

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = buyerRoleId, Name = "Buyer", IsDeleted = false, CreatedAt = now },
            new Role { Id = sellerRoleId, Name = "Seller", IsDeleted = false, CreatedAt = now },
            new Role { Id = adminRoleId, Name = "Admin", IsDeleted = false, CreatedAt = now },
            new Role { Id = shipperRoleId, Name = "Shipper", IsDeleted = false, CreatedAt = now }
        );

        var adminUserId = new Guid("00000000-0000-0000-0001-000000000001");
        var buyerUserId = new Guid("00000000-0000-0000-0001-000000000002");
        var shipperUserId = new Guid("00000000-0000-0000-0001-000000000003");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = adminUserId,
                Email = "admin@juratifact.com",
                HashedPassword = "eze6Lv1VCfFUF4bgfxzZ1g==:HH2e9+wzsnyPpOvyCaqCcg==",
                FullName = "System Administrator",
                PhoneNumber = "0900000001",
                UserName = "admin",
                IsVerify = true,
                TrustScore = 0,
                TotalTrustScore = 0,
                SellerReviewAmount = 0,
                IsDeleted = false,
                CreatedAt = now
            },
            new User
            {
                Id = buyerUserId,
                Email = "buyer@juratifact.com",
                HashedPassword = "LUh6Fk4qUZC+t2FT4xN7dQ==:P7wyGoWX+dmPHlwLDQEMwQ==",
                FullName = "Default Buyer",
                PhoneNumber = "0900000002",
                UserName = "buyer",
                Address = "123 Buyer Street, Ho Chi Minh City",
                IsVerify = true,
                TrustScore = 0,
                TotalTrustScore = 0,
                SellerReviewAmount = 0,
                IsDeleted = false,
                CreatedAt = now
            },
            new User
            {
                Id = shipperUserId,
                Email = "shipper@juratifact.com",
                HashedPassword = "7UFM1vLmaQQvP34bNdYAtw==:yW1WTThpMvifMtUfKQbFmA==",
                FullName = "Default Shipper",
                PhoneNumber = "0900000003",
                UserName = "shipper",
                Address = "456 Shipper Street, Ho Chi Minh City",
                IsVerify = true,
                TrustScore = 0,
                TotalTrustScore = 0,
                SellerReviewAmount = 0,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id = new Guid("00000000-0000-0000-0003-000000000001"),
                UserId = adminUserId,
                RoleId = adminRoleId,
                IsDeleted = false,
                CreatedAt = now
            },
            new UserRole
            {
                Id = new Guid("00000000-0000-0000-0003-000000000002"),
                UserId = buyerUserId,
                RoleId = buyerRoleId,
                IsDeleted = false,
                CreatedAt = now
            },
            new UserRole
            {
                Id = new Guid("00000000-0000-0000-0003-000000000003"),
                UserId = shipperUserId,
                RoleId = shipperRoleId,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        modelBuilder.Entity<Wallet>().HasData(
            new Wallet
            {
                Id = new Guid("00000000-0000-0000-0002-000000000002"),
                UserId = buyerUserId,
                Balance = 0,
                PendingBalance = 0,
                IsDeleted = false,
                CreatedAt = now
            },
            new Wallet
            {
                Id = new Guid("00000000-0000-0000-0002-000000000003"),
                UserId = shipperUserId,
                Balance = 0,
                PendingBalance = 0,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        modelBuilder.Entity<Cart>().HasData(
            new Cart
            {
                Id = new Guid("00000000-0000-0000-0004-000000000001"),
                UserId = buyerUserId,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = new Guid("00000000-0000-0000-0005-000000000001"),
                Name = "Electronics",
                IsDeleted = false,
                CreatedAt = now
            },
            new Category
            {
                Id = new Guid("00000000-0000-0000-0005-000000000002"),
                Name = "Fashion",
                IsDeleted = false,
                CreatedAt = now
            },
            new Category
            {
                Id = new Guid("00000000-0000-0000-0005-000000000003"),
                Name = "Home & Living",
                IsDeleted = false,
                CreatedAt = now
            },
            new Category
            {
                Id = new Guid("00000000-0000-0000-0005-000000000004"),
                Name = "Books",
                IsDeleted = false,
                CreatedAt = now
            },
            new Category
            {
                Id = new Guid("00000000-0000-0000-0005-000000000005"),
                Name = "Collectibles",
                IsDeleted = false,
                CreatedAt = now
            }
        );
    }

    private static void SeedLegacyData(ModelBuilder modelBuilder)
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // ════════════════════════════════════════
        // ROLES
        // ════════════════════════════════════════
        var buyerRoleId  = new Guid("00000000-0000-0000-0000-000000000001");
        var sellerRoleId = new Guid("00000000-0000-0000-0000-000000000002");
        var adminRoleId  = new Guid("00000000-0000-0000-0000-000000000003");
        var shipperRoleId = new Guid("00000000-0000-0000-0000-000000000004");

        
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = buyerRoleId,  Name = "Buyer",  IsDeleted = false, CreatedAt = now },
            new Role { Id = sellerRoleId, Name = "Seller", IsDeleted = false, CreatedAt = now },
            new Role { Id = adminRoleId,  Name = "Admin",  IsDeleted = false, CreatedAt = now },
            new Role {Id = shipperRoleId,  Name = "Shipper",  IsDeleted = false, CreatedAt = now}
        );

        // ════════════════════════════════════════
        // ADMIN USER  (không có Cart / Wallet)
        // Password: Admin@123
        // ════════════════════════════════════════
        var adminUserId = new Guid("00000000-0000-0000-0001-000000000001");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id                 = adminUserId,
                Email              = "admin@juratifact.com",
                HashedPassword     = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdyMnd.TdnMH4Oy", // Admin@123
                FullName           = "System Administrator",
                PhoneNumber        = "0000000000",
                UserName           = "admin",
                IsVerify           = true,
                TrustScore         = 0,
                TotalTrustScore    = 0,
                SellerReviewAmount = 0,
                IsDeleted          = false,
                CreatedAt          = now
            }
        );

        // Admin → role Admin
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id        = new Guid("00000000-0000-0000-0003-000000000001"),
                UserId    = adminUserId,
                RoleId    = adminRoleId,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        // ════════════════════════════════════════
        // BUYER USER  (có Cart + Wallet)
        // Password: Buyer@123
        // ════════════════════════════════════════
        var buyerUserId = new Guid("00000000-0000-0000-0001-000000000002");

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id                 = buyerUserId,
                Email              = "buyer@juratifact.com",
                HashedPassword     = "$2a$12$K9Wr2x8bRvLmDpTqNzY3OeI1hXsGcAf7JuBvE5MnPd6oQkWyHtZ4.", // Buyer@123
                FullName           = "Default Buyer",
                PhoneNumber        = "0000000001",
                UserName           = "buyer",
                IsVerify           = true,
                TrustScore         = 0,
                TotalTrustScore    = 0,
                SellerReviewAmount = 0,
                IsDeleted          = false,
                CreatedAt          = now
            }
        );

        // Buyer → Wallet
        modelBuilder.Entity<Wallet>().HasData(
            new Wallet
            {
                Id             = new Guid("00000000-0000-0000-0002-000000000002"),
                UserId         = buyerUserId,
                Balance        = 0,
                PendingBalance = 0,
                IsDeleted      = false,
                CreatedAt      = now
            }
        );

        // Buyer → Cart
        modelBuilder.Entity<Cart>().HasData(
            new Cart
            {
                Id        = new Guid("00000000-0000-0000-0004-000000000001"),
                UserId    = buyerUserId,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        // Buyer → role Buyer
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id        = new Guid("00000000-0000-0000-0003-000000000002"),
                UserId    = buyerUserId,
                RoleId    = buyerRoleId,
                IsDeleted = false,
                CreatedAt = now
            }
        );
    }
}
