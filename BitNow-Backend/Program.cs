using BitNow_Backend.DAL;
using BitNow_Backend.BLL.IServices;
using BitNow_Backend.BLL.Services;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;

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

builder.Services.AddScoped<IFavoriteSellerRepository, FavoriteSellerRepository>();
builder.Services.AddScoped<IFavoriteSellerService, FavoriteSellerService>();
// Bids
builder.Services.AddScoped<IBidRepository, BidRepository>();
builder.Services.AddScoped<IBidService, BidService>();

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
		policy.AllowAnyOrigin()
			  .AllowAnyMethod()
			  .AllowAnyHeader();
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

app.MapControllers();
// SignalR hubs
app.MapHub<BitNow_Backend.RealTime.AuctionHub>("/hubs/auction");
app.MapHub<BitNow_Backend.RealTime.MessageHub>("/hubs/messages");

// Seed admin from configuration
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
    }
    catch (Exception)
    {
        // swallow seeding errors to not block app startup
    }
}

app.Run();
