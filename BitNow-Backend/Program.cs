using BitNow_Backend.DAL;
using BitNow_Backend.BLL.IServices;
using BitNow_Backend.BLL.Services;
using BitNow_Backend.BLL.Payment;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.FileProviders;
using BitNow_Backend.Services;
using BitNow_Backend.RealTime;
using BitNow_Backend.BLL.BackgroundServices;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
// DAL: EF DbContext registration
builder.Services.AddDbContext<BidNowDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});

// BLL: Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailVerificationRepository, EmailVerificationRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<IAuctionService, AuctionService>();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IAuctionChatService, AuctionChatService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
// Dispute
builder.Services.AddScoped<BitNow_Backend.DAL.IRepositories.IDisputeRepository, BitNow_Backend.DAL.Repositories.DisputeRepository>();
builder.Services.AddScoped<IDisputeService, BitNow_Backend.BLL.Services.DisputeService>();

builder.Services.AddScoped<IFavoriteSellerRepository, FavoriteSellerRepository>();
builder.Services.AddScoped<IFavoriteSellerService, FavoriteSellerService>();
// Search keywords
builder.Services.AddScoped<ISearchKeywordRepository, SearchKeywordRepository>();
builder.Services.AddScoped<ISearchKeywordService, SearchKeywordService>();
// User auction views
builder.Services.AddScoped<IUserAuctionViewRepository, UserAuctionViewRepository>();
builder.Services.AddScoped<IUserAuctionViewService, UserAuctionViewService>();

// Ratings
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();

// File Upload Service
builder.Services.AddScoped<BitNow_Backend.Services.IFileUploadService, BitNow_Backend.Services.FileUploadService>();
// Bids
builder.Services.AddScoped<IBidRepository, BidRepository>();
builder.Services.AddScoped<IBidService, BidService>();
// Auto Bids
builder.Services.AddScoped<IAutoBidRepository, AutoBidRepository>();
builder.Services.AddScoped<IAutoBidService, AutoBidService>();
// Bid Notification
builder.Services.AddScoped<BitNow_Backend.BLL.IServices.IBidNotificationService, BitNow_Backend.Services.BidNotificationService>();
// Notification Hub Service
builder.Services.AddScoped<INotificationHub, NotificationHubService>();
    // Admin Stats
    builder.Services.AddScoped<IAdminStatsService, AdminStatsService>();
    // Seller Stats
    builder.Services.AddScoped<ISellerStatsService, SellerStatsService>();
// Platform Analytics
builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();


// Register Background Service
builder.Services.AddHostedService<CleanupBackgroundService>();

builder.Services.AddHostedService<AuctionFinalizationBackgroundService>();



// AI Recommendations - Vector-based
Console.OutputEncoding = Encoding.UTF8;

builder.Services.AddScoped<IPineconeService, PineconeService>();
builder.Services.AddScoped<IVectorSyncService, VectorSyncService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// HttpClient for LM Studio (local embedding service)
builder.Services.AddHttpClient("LMStudio");

// HttpClient for Pinecone
builder.Services.AddHttpClient("Pinecone");

// Payment Services
builder.Services.AddScoped<IPayOsService, PayOsService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Background Service for Auction Status Updates
builder.Services.AddHostedService<AuctionStatusUpdateService>();


// Add services to the container.
builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();

// Redis (cache + pub/sub if needed)
var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
	try
	{
		var options = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
		options.AbortOnConnectFail = false; // allow startup even if redis not ready
		var mux = ConnectionMultiplexer.Connect(options);
		builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
		builder.Services.AddStackExchangeRedisCache(cfg => { cfg.Configuration = redisConnectionString; });
	}
	catch
	{
		// If Redis is unavailable, continue without registering it
	}
}

// Add CORS
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		// When using credentials, we must specify exact origins, not wildcard
		policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
			  .AllowAnyMethod()
			  .AllowAnyHeader()
			  .AllowCredentials(); // Required when frontend uses credentials: 'include'
	});
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// Use CORS early to handle preflight before any redirects
app.UseCors("AllowAll");


// Avoid redirecting preflight requests in development (causes CORS failure)
if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}

app.UseAuthorization();

 

// Serve static files from wwwroot folder (nơi lưu ảnh)
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");

// Đảm bảo thư mục wwwroot tồn tại
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

// Configure static file serving from wwwroot
// Ảnh được lưu trong wwwroot/uploads, truy cập qua /images/uploads/filename
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath),
    RequestPath = "/images"
});


app.MapControllers();
// SignalR hubs
app.MapHub<BitNow_Backend.RealTime.AuctionHub>("/hubs/auction");
app.MapHub<BitNow_Backend.RealTime.MessageHub>("/hubs/messages");

// Seed admin from configuration and categories
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var ctx = services.GetRequiredService<BidNowDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        var fullName = config["Admin:FullName"] ?? "Administrator";

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var existing = await ctx.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == email);
            if (existing == null)
            {
                var admin = new BitNow_Backend.DAL.Models.User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ReputationScore = 0.00m,
                    TotalRatings = 0,
                    TotalSales = 0,
                    TotalPurchases = 0
                };
                ctx.Users.Add(admin);
                await ctx.SaveChangesAsync();

                ctx.UserRoles.Add(new BitNow_Backend.DAL.Models.UserRole { UserId = admin.Id, Role = "admin", CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
            }
            else if (!existing.UserRoles.Any(r => r.Role == "admin"))
            {
                ctx.UserRoles.Add(new BitNow_Backend.DAL.Models.UserRole { UserId = existing.Id, Role = "admin", CreatedAt = DateTime.UtcNow });
                await ctx.SaveChangesAsync();
            }
        }

        // Seed categories
        var categories = new[]
        {
            new { Name = "Điện tử", Slug = "dien-tu", Description = "Điện thoại, máy tính, thiết bị điện tử" },
            new { Name = "Nghệ thuật", Slug = "nghe-thuat", Description = "Tranh vẽ, tác phẩm nghệ thuật, đồ trang trí" },
            new { Name = "Sưu tầm", Slug = "suu-tam", Description = "Đồ cổ, tem, tiền xu, đồ sưu tầm" },
            new { Name = "Trang sức", Slug = "trang-suc", Description = "Vòng tay, nhẫn, dây chuyền, đồ trang sức" },
            new { Name = "Xe cộ", Slug = "xe-co", Description = "Ô tô, xe máy, xe đạp, phương tiện" },
            new { Name = "Bất động sản", Slug = "bat-dong-san", Description = "Nhà đất, căn hộ, bất động sản" },
            new { Name = "Nhạc cụ", Slug = "nhac-cu", Description = "Đàn, trống, kèn, nhạc cụ các loại" },
            new { Name = "Nhiếp ảnh", Slug = "nhiep-anh", Description = "Máy ảnh, ống kính, thiết bị nhiếp ảnh" },
        };

        foreach (var cat in categories)
        {
            var existingCategory = await ctx.Categories.FirstOrDefaultAsync(c => c.Slug == cat.Slug);
            if (existingCategory == null)
            {
                ctx.Categories.Add(new BitNow_Backend.DAL.Models.Category
                {
                    Name = cat.Name,
                    Slug = cat.Slug,
                    Description = cat.Description,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await ctx.SaveChangesAsync();
    }
    catch (Exception)
    {
        // swallow seeding errors to not block app startup
    }
}

app.Run();
