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

#region DATABASE
builder.Services.AddDbContext<BidNowDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});
#endregion

#region SERVICES
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
builder.Services.AddScoped<IDisputeService, DisputeService>();
builder.Services.AddScoped<BitNow_Backend.DAL.IRepositories.IDisputeRepository, DisputeRepository>();
builder.Services.AddScoped<IFavoriteSellerRepository, FavoriteSellerRepository>();
builder.Services.AddScoped<IFavoriteSellerService, FavoriteSellerService>();
builder.Services.AddScoped<ISearchKeywordRepository, SearchKeywordRepository>();
builder.Services.AddScoped<ISearchKeywordService, SearchKeywordService>();
builder.Services.AddScoped<IUserAuctionViewRepository, UserAuctionViewRepository>();
builder.Services.AddScoped<IUserAuctionViewService, UserAuctionViewService>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IBidRepository, BidRepository>();
builder.Services.AddScoped<IBidService, BidService>();
builder.Services.AddScoped<IAutoBidRepository, AutoBidRepository>();
builder.Services.AddScoped<IAutoBidService, AutoBidService>();
builder.Services.AddScoped<IBidNotificationService, BidNotificationService>();
builder.Services.AddScoped<INotificationHub, NotificationHubService>();
builder.Services.AddScoped<IAdminStatsService, AdminStatsService>();
builder.Services.AddScoped<ISellerStatsService, SellerStatsService>();
builder.Services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();
#endregion

#region BACKGROUND
builder.Services.AddHostedService<CleanupBackgroundService>();
builder.Services.AddHostedService<AuctionFinalizationBackgroundService>();
builder.Services.AddHostedService<AuctionStatusUpdateService>();
#endregion

#region AI
Console.OutputEncoding = Encoding.UTF8;
builder.Services.AddScoped<IPineconeService, PineconeService>();
builder.Services.AddScoped<IVectorSyncService, VectorSyncService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddHttpClient("LMStudio");
builder.Services.AddHttpClient("Pinecone");
#endregion

#region PAYMENT
builder.Services.AddScoped<IPayOsService, PayOsService>();
builder.Services.AddScoped<IOrderService, OrderService>();
#endregion

#region REDIS
var redisConnectionString = builder.Configuration.GetSection("Redis")["ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    try
    {
        var redisOptions = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        var mux = ConnectionMultiplexer.Connect(redisOptions);
        builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
        builder.Services.AddStackExchangeRedisCache(cfg =>
        {
            cfg.Configuration = redisConnectionString;
        });
    }
    catch { }
}
#endregion

#region CONTROLLERS + SIGNALR + CORS
builder.Services.AddControllers();

builder.Services.AddSignalR(options =>
{
    // KeepAliveInterval: Server gửi ping mỗi 15s để giữ connection
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    // ClientTimeoutInterval: Client phải respond trong 500s, nếu không sẽ disconnect
    // Với long polling, client sẽ tự reconnect trước khi timeout
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(500);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    // Note: Long polling timeout được xử lý ở client-side (trong auctionHub.ts)
    // Server không có cấu hình riêng cho long polling timeout
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000",
                "https://bitnow.io.vn",
                "https://www.bitnow.io.vn"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
#endregion

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

#region PIPELINE (QUAN TRỌNG)
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();                 // ⚠️ BẮT BUỘC
app.UseCors("AllowFrontend");     // ⚠️ BẮT BUỘC – SAU UseRouting

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootPath),
    RequestPath = "/images"
});

app.MapControllers();

// Map SignalR hubs - CORS sẽ tự động được áp dụng từ middleware
app.MapHub<AuctionHub>("/hubs/auction")
    .RequireCors("AllowFrontend");
app.MapHub<MessageHub>("/hubs/messages")
    .RequireCors("AllowFrontend");
#endregion

#region SEED DATA
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
            var existing = await ctx.Users.Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existing == null)
            {
                var admin = new BitNow_Backend.DAL.Models.User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    FullName = fullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                ctx.Users.Add(admin);
                await ctx.SaveChangesAsync();

                ctx.UserRoles.Add(new BitNow_Backend.DAL.Models.UserRole
                {
                    UserId = admin.Id,
                    Role = "admin",
                    CreatedAt = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();
            }
        }
    }
    catch { }
}
#endregion

app.Run();
