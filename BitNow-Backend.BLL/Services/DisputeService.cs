using BitNow_Backend.BLL.IServices;
using BitNow_Backend.BLL.Payment;
using BitNow_Backend.DAL;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitNow_Backend.BLL.Services;

public class DisputeService : IDisputeService
{
    private readonly IDisputeRepository _disputeRepository;
    private readonly IOrderService _orderService;
    private readonly BidNowDbContext _dbContext;
    private readonly ILogger<DisputeService> _logger;

    public DisputeService(
        IDisputeRepository disputeRepository,
        IOrderService orderService,
        BidNowDbContext dbContext,
        ILogger<DisputeService> logger)
    {
        _disputeRepository = disputeRepository;
        _orderService = orderService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DisputeDto?> GetByIdAsync(int id)
    {
        var dispute = await _disputeRepository.GetByIdAsync(id);
        return dispute == null ? null : MapToDto(dispute);
    }

    public async Task<DisputeDto?> GetByOrderIdAsync(int orderId)
    {
        var dispute = await _disputeRepository.GetByOrderIdAsync(orderId);
        return dispute == null ? null : MapToDto(dispute);
    }

    public async Task<IEnumerable<DisputeDto>> GetAllAsync()
    {
        var disputes = await _disputeRepository.GetAllAsync();
        return disputes.Select(MapToDto);
    }

    public async Task<IEnumerable<DisputeDto>> GetByStatusAsync(string status)
    {
        var disputes = await _disputeRepository.GetByStatusAsync(status);
        return disputes.Select(MapToDto);
    }

    public async Task<IEnumerable<DisputeDto>> GetByBuyerIdAsync(int buyerId)
    {
        var disputes = await _disputeRepository.GetByBuyerIdAsync(buyerId);
        return disputes.Select(MapToDto);
    }

    public async Task<IEnumerable<DisputeDto>> GetBySellerIdAsync(int sellerId)
    {
        var disputes = await _disputeRepository.GetBySellerIdAsync(sellerId);
        return disputes.Select(MapToDto);
    }

    public async Task<DisputeDto> CreateDisputeAsync(CreateDisputeDto dto, int buyerId)
    {
        var order = await _orderService.GetOrderByIdAsync(dto.OrderId);
        if (order == null)
            throw new ArgumentException("Order not found");

        if (order.BuyerId != buyerId)
            throw new UnauthorizedAccessException("Only the buyer can create a dispute for this order");

        // Check if dispute already exists
        var existingDispute = await _disputeRepository.GetByOrderIdAsync(dto.OrderId);
        if (existingDispute != null)
            throw new InvalidOperationException("Dispute already exists for this order");

        var dispute = new Dispute
        {
            OrderId = dto.OrderId,
            BuyerId = buyerId,
            SellerId = order.SellerId,
            Reason = dto.Reason,
            Description = dto.Description,
            Status = "pending",
            CreatedAt = DateTime.Now
        };

        var createdDispute = await _disputeRepository.AddAsync(dispute);
        _logger.LogInformation("Dispute {DisputeId} created for order {OrderId}", createdDispute.Id, dto.OrderId);

        // Reload dispute with all navigation properties
        var reloadedDispute = await _disputeRepository.GetByIdAsync(createdDispute.Id);
        if (reloadedDispute == null)
            throw new InvalidOperationException($"Failed to retrieve created dispute {createdDispute.Id}");

        return MapToDto(reloadedDispute);
    }

    public async Task<DisputeDto> StartReviewAsync(int disputeId, int adminId)
    {
        var dispute = await _disputeRepository.GetByIdAsync(disputeId);
        if (dispute == null)
            throw new ArgumentException("Dispute not found");

        if (dispute.Status != "pending")
            throw new InvalidOperationException("Dispute is not in pending status");

        dispute.Status = "in_review";
        dispute.ResolvedBy = adminId;

        await _disputeRepository.UpdateAsync(dispute);
        _logger.LogInformation("Dispute {DisputeId} started review by admin {AdminId}", disputeId, adminId);

        return MapToDto(dispute);
    }

    public async Task<DisputeDto> ResolveDisputeAsync(int disputeId, ResolveDisputeDto dto, int adminId)
    {
        var dispute = await _disputeRepository.GetByIdAsync(disputeId);
        if (dispute == null)
            throw new ArgumentException("Dispute not found");

        if (dispute.Status != "in_review" && dispute.Status != "pending")
            throw new InvalidOperationException("Dispute cannot be resolved in current status");

        var order = await _orderService.GetOrderByIdAsync(dispute.OrderId);
        if (order == null)
            throw new ArgumentException("Order not found");

        // Update dispute status
        if (dto.Winner.ToLower() == "buyer")
        {
            dispute.Status = "buyer_won";
            dispute.Resolution = "refunded_to_buyer";
        }
        else if (dto.Winner.ToLower() == "seller")
        {
            dispute.Status = "seller_won";
            dispute.Resolution = "released_to_seller";
        }
        else
        {
            throw new ArgumentException("Winner must be 'buyer' or 'seller'");
        }

        dispute.ResolvedBy = adminId;
        dispute.ResolvedAt = DateTime.Now;
        dispute.AdminNotes = dto.AdminNotes;

        // Update order and payment status
        if (dto.Winner.ToLower() == "buyer")
        {
            // Refund to buyer
            await _orderService.UpdateOrderStatusAsync(dispute.OrderId, "cancelled");
            
            // Update payment status
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == dispute.OrderId);
            if (payment != null)
            {
                payment.PaymentStatus = "refunded_to_buyer";
                payment.RefundedAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
        }
        else
        {
            // Release to seller
            await _orderService.UpdateOrderStatusAsync(dispute.OrderId, "completed");
            
            // Update payment status
            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == dispute.OrderId);
            if (payment != null)
            {
                payment.PaymentStatus = "released_to_seller";
                payment.ReleasedAt = DateTime.Now;
                payment.UpdatedAt = DateTime.Now;
                await _dbContext.SaveChangesAsync();
            }
        }

        await _disputeRepository.UpdateAsync(dispute);
        _logger.LogInformation("Dispute {DisputeId} resolved by admin {AdminId}, winner: {Winner}", disputeId, adminId, dto.Winner);

        return MapToDto(dispute);
    }

    private DisputeDto MapToDto(Dispute dispute)
    {
        try
        {
            return new DisputeDto
            {
                Id = dispute.Id,
                OrderId = dispute.OrderId,
                BuyerId = dispute.BuyerId,
                BuyerName = dispute.Buyer?.FullName ?? "Unknown",
                SellerId = dispute.SellerId,
                SellerName = dispute.Seller?.FullName ?? "Unknown",
                Reason = dispute.Reason,
                Description = dispute.Description,
                Status = dispute.Status,
                Resolution = dispute.Resolution,
                ResolvedBy = dispute.ResolvedBy,
                ResolverName = dispute.Resolver?.FullName,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                ClosedAt = dispute.ClosedAt,
                AdminNotes = dispute.AdminNotes,
                AuctionTitle = dispute.Order?.Auction?.Item?.Title ?? null,
                OrderAmount = dispute.Order?.FinalPrice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping dispute {DisputeId} to DTO", dispute.Id);
            // Return basic DTO without navigation properties if mapping fails
            return new DisputeDto
            {
                Id = dispute.Id,
                OrderId = dispute.OrderId,
                BuyerId = dispute.BuyerId,
                BuyerName = "Unknown",
                SellerId = dispute.SellerId,
                SellerName = "Unknown",
                Reason = dispute.Reason,
                Description = dispute.Description,
                Status = dispute.Status,
                Resolution = dispute.Resolution,
                ResolvedBy = dispute.ResolvedBy,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                ClosedAt = dispute.ClosedAt,
                AdminNotes = dispute.AdminNotes
            };
        }
    }
}

