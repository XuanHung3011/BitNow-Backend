using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Payment;

public class OrderService : IOrderService
{
    private readonly BidNowDbContext _dbContext;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        BidNowDbContext dbContext,
        IAuctionRepository auctionRepository,
        IUserRepository userRepository,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _auctionRepository = auctionRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderForWinnerAsync(int auctionId, int winnerId, decimal finalPrice)
    {
        try
        {
            // Check if order already exists for this auction
            var existingOrder = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.AuctionId == auctionId);

            if (existingOrder != null)
            {
                _logger.LogWarning("Order already exists for auction {AuctionId}", auctionId);
                return MapToDto(existingOrder);
            }

            // Get auction with seller info
            var auction = await _auctionRepository.GetByIdAsync(auctionId);
            if (auction == null)
            {
                throw new InvalidOperationException($"Auction {auctionId} not found");
            }

            // Create order
            var order = new Order
            {
                AuctionId = auctionId,
                BuyerId = winnerId,
                SellerId = auction.SellerId,
                FinalPrice = finalPrice,
                OrderStatus = "awaiting_payment", // Winner needs to pay
                CreatedAt = DateTime.Now
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} created for auction {AuctionId}, winner {WinnerId}", order.Id, auctionId, winnerId);

            // Reload order with all navigation properties for mapping
            var createdOrder = await _dbContext.Orders
                .Include(o => o.Auction)
                .Include(o => o.Buyer)
                .Include(o => o.Seller)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (createdOrder == null)
            {
                throw new InvalidOperationException($"Failed to retrieve created order {order.Id}");
            }

            return MapToDto(createdOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for auction {AuctionId}", auctionId);
            throw;
        }
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Auction)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByAuctionIdAsync(int auctionId)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Auction)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.Payment)
            .FirstOrDefaultAsync(o => o.AuctionId == auctionId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
    {
        try
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                return false;
            }

            order.OrderStatus = status;
            order.UpdatedAt = DateTime.Now;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId} status", orderId);
            return false;
        }
    }

    public async Task<List<OrderDto>> GetOrdersByBuyerIdAsync(int buyerId)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Auction)
            .ThenInclude(a => a.Item)
            .Include(o => o.Seller)
            .Include(o => o.Payment)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<OrderDto>> GetOrdersBySellerIdAsync(int sellerId)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.Auction)
            .ThenInclude(a => a.Item)
            .Include(o => o.Buyer)
            .Include(o => o.Payment)
            .Where(o => o.SellerId == sellerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateShippingInfoAsync(int orderId, string trackingNumber, string? shippingCompany, string? shippingAddress)
    {
        try
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for shipping update", orderId);
                return false;
            }

            // Only allow shipping update if order is in awaiting_shipment status
            if (order.OrderStatus != "awaiting_shipment")
            {
                _logger.LogWarning("Cannot update shipping for order {OrderId} with status {Status}", orderId, order.OrderStatus);
                return false;
            }

            order.TrackingNumber = trackingNumber;
            order.ShippingCompany = shippingCompany;
            order.ShippingAddress = shippingAddress;
            order.ShippedAt = DateTime.Now;
            order.OrderStatus = "shipped";
            order.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Shipping info updated for order {OrderId}: TrackingNumber={TrackingNumber}, Company={Company}", 
                orderId, trackingNumber, shippingCompany);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipping info for order {OrderId}", orderId);
            return false;
        }
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            AuctionId = order.AuctionId,
            BuyerId = order.BuyerId,
            SellerId = order.SellerId,
            FinalPrice = order.FinalPrice,
            OrderStatus = order.OrderStatus,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CancelledAt = order.CancelledAt,
            CancelReason = order.CancelReason,
            TrackingNumber = order.TrackingNumber,
            ShippingCompany = order.ShippingCompany,
            ShippedAt = order.ShippedAt,
            ShippingAddress = order.ShippingAddress,
            Payment = order.Payment != null ? new PaymentDto
            {
                Id = order.Payment.Id,
                OrderId = order.Payment.OrderId,
                Amount = order.Payment.Amount,
                PaymentStatus = order.Payment.PaymentStatus,
                PaymentMethod = order.Payment.PaymentMethod,
                TransactionId = order.Payment.TransactionId,
                PaymentProvider = order.Payment.PaymentProvider,
                PaidAt = order.Payment.PaidAt,
                ReleasedAt = order.Payment.ReleasedAt,
                RefundedAt = order.Payment.RefundedAt,
                CreatedAt = order.Payment.CreatedAt,
                UpdatedAt = order.Payment.UpdatedAt,
                Notes = order.Payment.Notes
            } : null
        };
    }
}

