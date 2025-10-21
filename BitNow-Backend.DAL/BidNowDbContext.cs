using Microsoft.EntityFrameworkCore;
using BitNow_Backend.DAL.Models;

namespace BitNow_Backend.DAL;

public partial class BidNowDbContext : DbContext
{
    public BidNowDbContext()
    {
    }

    public BidNowDbContext(DbContextOptions<BidNowDbContext> options) : base(options)
    {
    }

    public virtual DbSet<Auction> Auctions { get; set; }
    public virtual DbSet<AuctionHistory> AuctionHistories { get; set; }
    public virtual DbSet<AutoBid> AutoBids { get; set; }
    public virtual DbSet<Bid> Bids { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<ContactMessage> ContactMessages { get; set; }
    public virtual DbSet<FavoriteSeller> FavoriteSellers { get; set; }
    public virtual DbSet<Item> Items { get; set; }
    public virtual DbSet<Message> Messages { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Rating> Ratings { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserRole> UserRoles { get; set; }
    public virtual DbSet<Watchlist> Watchlists { get; set; }
    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The original fluent mappings are kept in the API version; copy them here 1:1
        modelBuilder.Entity<Auction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Auctions__3213E83F5033A638");
            entity.HasIndex(e => e.Status, "idx_auctions_status");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BidCount).HasDefaultValue(0).HasColumnName("bid_count");
            entity.Property(e => e.BuyNowPrice).HasColumnType("decimal(18, 2)").HasColumnName("buy_now_price");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.CurrentBid).HasColumnType("decimal(18, 2)").HasColumnName("current_bid");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.StartingBid).HasColumnType("decimal(18, 2)").HasColumnName("starting_bid");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
            entity.Property(e => e.WinnerId).HasColumnName("winner_id");
            entity.HasOne(d => d.Item).WithMany(p => p.Auctions).HasForeignKey(d => d.ItemId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Auctions__item_i__534D60F1");
            entity.HasOne(d => d.Seller).WithMany(p => p.AuctionSellers).HasForeignKey(d => d.SellerId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Auctions__seller__5441852A");
            entity.HasOne(d => d.Winner).WithMany(p => p.AuctionWinners).HasForeignKey(d => d.WinnerId).HasConstraintName("FK__Auctions__winner__5535A963");
        });

        modelBuilder.Entity<AuctionHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuctionH__3213E83F8F90DB51");
            entity.ToTable("AuctionHistory");
            entity.HasIndex(e => e.SellerId, "idx_history_seller");
            entity.HasIndex(e => e.WinnerId, "idx_history_winner");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuctionId).HasColumnName("auction_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CompletedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("completed_at");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.FinalBid).HasColumnType("decimal(18, 2)").HasColumnName("final_bid");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.StartingBid).HasColumnType("decimal(18, 2)").HasColumnName("starting_bid");
            entity.Property(e => e.Title).HasMaxLength(255).HasColumnName("title");
            entity.Property(e => e.TotalBids).HasDefaultValue(0).HasColumnName("total_bids");
            entity.Property(e => e.WinnerId).HasColumnName("winner_id");
        });

        modelBuilder.Entity<AutoBid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AutoBids__3213E83FE492B6C8");
            entity.HasIndex(e => new { e.AuctionId, e.UserId }, "UQ__AutoBids__C46C653189F6A89F").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuctionId).HasColumnName("auction_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.MaxAmount).HasColumnType("decimal(18, 2)").HasColumnName("max_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasOne(d => d.Auction).WithMany(p => p.AutoBids).HasForeignKey(d => d.AuctionId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__AutoBids__auctio__60A75C0F");
            entity.HasOne(d => d.User).WithMany(p => p.AutoBids).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__AutoBids__user_i__619B8048");
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Bids__3213E83F55D6A6DC");
            entity.HasIndex(e => e.AuctionId, "idx_bids_auction");
            entity.HasIndex(e => e.BidderId, "idx_bids_bidder");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("amount");
            entity.Property(e => e.AuctionId).HasColumnName("auction_id");
            entity.Property(e => e.BidTime).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("bid_time");
            entity.Property(e => e.BidderId).HasColumnName("bidder_id");
            entity.Property(e => e.IsAutoBid).HasDefaultValue(false).HasColumnName("is_auto_bid");
            entity.HasOne(d => d.Auction).WithMany(p => p.Bids).HasForeignKey(d => d.AuctionId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Bids__auction_id__59FA5E80");
            entity.HasOne(d => d.Bidder).WithMany(p => p.Bids).HasForeignKey(d => d.BidderId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Bids__bidder_id__5AEE82B9");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3213E83F277DE0DA");
            entity.HasIndex(e => e.Slug, "UQ__Categori__32DD1E4CD9936ABD").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.Icon).HasMaxLength(50).HasColumnName("icon");
            entity.Property(e => e.Name).HasMaxLength(100).HasColumnName("name");
            entity.Property(e => e.Slug).HasMaxLength(100).HasColumnName("slug");
        });

        modelBuilder.Entity<ContactMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ContactM__3213E83FBD190A4F");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdminReply).HasColumnName("admin_reply");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Name).HasMaxLength(255).HasColumnName("name");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending").HasColumnName("status");
            entity.Property(e => e.Subject).HasMaxLength(255).HasColumnName("subject");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasOne(d => d.User).WithMany(p => p.ContactMessages).HasForeignKey(d => d.UserId).HasConstraintName("FK__ContactMe__user___73BA3083");
        });

        modelBuilder.Entity<FavoriteSeller>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Favorite__3213E83FB7177F4E");
            entity.HasIndex(e => new { e.BuyerId, e.SellerId }, "UQ__Favorite__DD51D1FAAF26F0F0").IsUnique();
            entity.HasIndex(e => e.BuyerId, "idx_favorites_buyer");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BuyerId).HasColumnName("buyer_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.HasOne(d => d.Buyer).WithMany(p => p.FavoriteSellerBuyers).HasForeignKey(d => d.BuyerId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__FavoriteS__buyer__00200768");
            entity.HasOne(d => d.Seller).WithMany(p => p.FavoriteSellerSellers).HasForeignKey(d => d.SellerId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__FavoriteS__selle__01142BA1");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Items__3213E83FB79E5DB3");
            entity.HasIndex(e => e.CategoryId, "idx_items_category");
            entity.HasIndex(e => e.SellerId, "idx_items_seller");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)").HasColumnName("base_price");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Condition).HasMaxLength(50).HasColumnName("condition");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Images).HasColumnName("images");
            entity.Property(e => e.Location).HasMaxLength(255).HasColumnName("location");
            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("pending").HasColumnName("status");
            entity.Property(e => e.Title).HasMaxLength(255).HasColumnName("title");
            entity.HasOne(d => d.Category).WithMany(p => p.Items).HasForeignKey(d => d.CategoryId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Items__category___4D94879B");
            entity.HasOne(d => d.Seller).WithMany(p => p.Items).HasForeignKey(d => d.SellerId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Items__seller_id__4CA06362");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Messages__3213E83FCD78A6BD");
            entity.HasIndex(e => e.ReceiverId, "idx_messages_receiver");
            entity.HasIndex(e => e.SenderId, "idx_messages_sender");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuctionId).HasColumnName("auction_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsRead).HasDefaultValue(false).HasColumnName("is_read");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.SentAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("sent_at");
            entity.HasOne(d => d.Auction).WithMany(p => p.Messages).HasForeignKey(d => d.AuctionId).HasConstraintName("FK__Messages__auctio__6E01572D");
            entity.HasOne(d => d.Receiver).WithMany(p => p.MessageReceivers).HasForeignKey(d => d.ReceiverId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Messages__receiv__6D0D32F4");
            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders).HasForeignKey(d => d.SenderId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Messages__sender__6C190EBB");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3213E83F0684F1AC");
            entity.HasIndex(e => e.UserId, "idx_notifications_user");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasDefaultValue(false).HasColumnName("is_read");
            entity.Property(e => e.Link).HasMaxLength(500).HasColumnName("link");
            entity.Property(e => e.Message).HasMaxLength(500).HasColumnName("message");
            entity.Property(e => e.Type).HasMaxLength(50).HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Notificat__user___05D8E0BE");
        });

        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Ratings__3213E83F00FD3E6D");
            entity.HasIndex(e => new { e.AuctionId, e.RaterId, e.RatedId }, "UQ__Ratings__6384321E0C855D8C").IsUnique();
            entity.HasIndex(e => e.RatedId, "idx_ratings_rated");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuctionId).HasColumnName("auction_id");
            entity.Property(e => e.Comment).HasMaxLength(1000).HasColumnName("comment");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.RatedId).HasColumnName("rated_id");
            entity.Property(e => e.RaterId).HasColumnName("rater_id");
            entity.Property(e => e.Rating1).HasColumnName("rating");
            entity.HasOne(d => d.Auction).WithMany(p => p.Ratings).HasForeignKey(d => d.AuctionId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Ratings__auction__797309D9");
            entity.HasOne(d => d.Rated).WithMany(p => p.RatingRateds).HasForeignKey(d => d.RatedId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Ratings__rated_i__7B5B524B");
            entity.HasOne(d => d.Rater).WithMany(p => p.RatingRaters).HasForeignKey(d => d.RaterId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Ratings__rater_i__7A672E12");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83FD002142C");
            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164AD34E528").IsUnique();
            entity.HasIndex(e => e.Email, "idx_users_email");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.FullName).HasMaxLength(255).HasColumnName("full_name");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
            entity.Property(e => e.PasswordHash).HasMaxLength(255).HasColumnName("password_hash");
            entity.Property(e => e.Phone).HasMaxLength(20).HasColumnName("phone");
            entity.Property(e => e.ReputationScore).HasDefaultValue(0.00m).HasColumnType("decimal(3, 2)").HasColumnName("reputation_score");
            entity.Property(e => e.TotalPurchases).HasDefaultValue(0).HasColumnName("total_purchases");
            entity.Property(e => e.TotalRatings).HasDefaultValue(0).HasColumnName("total_ratings");
            entity.Property(e => e.TotalSales).HasDefaultValue(0).HasColumnName("total_sales");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserRole__3213E83F823847CC");
            entity.HasIndex(e => new { e.UserId, e.Role }, "UQ__UserRole__31DDE51AC6C34E00").IsUnique();
            entity.HasIndex(e => e.UserId, "idx_roles_user");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("created_at");
            entity.Property(e => e.Role).HasMaxLength(20).HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasOne(d => d.User).WithMany(p => p.UserRoles).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__UserRoles__user___4316F928");
        });

        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Watchlis__3213E83F49C30639");
            entity.ToTable("Watchlist");
            entity.HasIndex(e => new { e.UserId, e.AuctionId }, "UQ__Watchlis__AB414F6AC4A841B0").IsUnique();
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddedAt).HasDefaultValueSql("(sysutcdatetime())").HasColumnName("added_at");
            entity.Property(e => e.AuctionId).HasColumnName("auction_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasOne(d => d.Auction).WithMany(p => p.Watchlists).HasForeignKey(d => d.AuctionId).HasConstraintName("FK__Watchlist__aucti__6754599E");
            entity.HasOne(d => d.User).WithMany(p => p.Watchlists).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("FK__Watchlist__user___66603565");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Token).HasMaxLength(255);
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.ExpiresAt);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}


