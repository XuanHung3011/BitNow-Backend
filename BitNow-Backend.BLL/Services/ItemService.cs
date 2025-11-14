using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitNow_Backend.BLL.Services
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;

        public ItemService(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<IEnumerable<ItemResponseDto>> GetAllApprovedItemsAsync()
        {
            var items = await _itemRepository.GetAllApprovedWithAuctionAsync();
            return items.Select(MapToResponseDto).ToList();
        }

        public async Task<IEnumerable<ItemResponseDto>> GetAllApprovedItemsPagedAsync(int page, int pageSize)
        {
            var items = await _itemRepository.GetAllApprovedWithAuctionPagedAsync(page, pageSize);
            return items.Select(MapToResponseDto).ToList();
        }

        public async Task<(IEnumerable<ItemResponseDto> items, int totalCount)> GetAllApprovedItemsWithCountAsync(int page, int pageSize)
        {
            var items = await _itemRepository.GetAllApprovedWithAuctionPagedAsync(page, pageSize);
            var totalCount = await _itemRepository.CountApprovedAsync();

            return (items.Select(MapToResponseDto).ToList(), totalCount);
        }

        public async Task<IEnumerable<ItemResponseDto>> SearchApprovedItemsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllApprovedItemsAsync();
            }

            var items = await _itemRepository.SearchApprovedWithAuctionAsync(searchTerm);
            return items.Select(MapToResponseDto).ToList();
        }

        public async Task<IEnumerable<ItemResponseDto>> SearchApprovedItemsPagedAsync(string searchTerm, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllApprovedItemsPagedAsync(page, pageSize);
            }

            var items = await _itemRepository.SearchApprovedWithAuctionPagedAsync(searchTerm, page, pageSize);
            return items.Select(MapToResponseDto).ToList();
        }

        public async Task<(IEnumerable<ItemResponseDto> items, int totalCount)> SearchApprovedItemsWithCountAsync(string searchTerm, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllApprovedItemsWithCountAsync(page, pageSize);
            }

            var items = await _itemRepository.SearchApprovedWithAuctionPagedAsync(searchTerm, page, pageSize);
            var totalCount = await _itemRepository.CountSearchApprovedAsync(searchTerm);

            return (items.Select(MapToResponseDto).ToList(), totalCount);
        }

        public async Task<(IEnumerable<ItemResponseDto> items, int totalCount)> FilterApprovedItemsAsync(ItemFilterDto filter, int page, int pageSize)
        {
            var items = await _itemRepository.FilterApprovedItemsAsync(filter, page, pageSize);
            var totalCount = await _itemRepository.CountFilteredApprovedAsync(filter);

            return (items.Select(MapToResponseDto).ToList(), totalCount);
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
        {
            return await _itemRepository.GetCategoriesAsync();
        }

        public async Task<IEnumerable<ItemResponseDto>> GetHotApprovedItemsAsync(int limit)
        {
            var items = await _itemRepository.GetHotApprovedActiveAuctionsAsync(limit);
            return items.Select(MapToResponseDto).ToList();
        }

        public async Task<PaginatedResult<ItemResponseDto>> GetAllItemsWithFilterAsync(ItemFilterAllDto filter)
        {
            var items = await _itemRepository.GetAllItemsWithFilterAsync(filter);
            var totalCount = await _itemRepository.CountAllItemsWithFilterAsync(filter);

            return new PaginatedResult<ItemResponseDto>
            {
                Data = items.Select(MapToResponseDto).ToList(),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<bool> ApproveItemAsync(int id)
        {
            return await _itemRepository.UpdateItemStatusAsync(id, "approved");
        }

        public async Task<bool> RejectItemAsync(int id)
        {
            return await _itemRepository.UpdateItemStatusAsync(id, "rejected");
        }

        public async Task<ItemResponseDto?> GetByIdAsync(int id)
        {
            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null)
            {
                return null;
            }
            return MapToResponseDto(item);
        }

        public async Task<ItemResponseDto?> CreateItemAsync(CreateItemDto dto, string? imagesPath = null)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new ArgumentException("Title is required");
            }

            if (dto.BasePrice <= 0)
            {
                throw new ArgumentException("BasePrice must be greater than 0");
            }

            // Check if category exists by getting categories
            var categories = await _itemRepository.GetCategoriesAsync();
            if (!categories.Any(c => c.Id == dto.CategoryId))
            {
                throw new ArgumentException("Category not found");
            }

            // Create item with pending status
            var item = new Item
            {
                SellerId = dto.SellerId,
                CategoryId = dto.CategoryId,
                Title = dto.Title,
                Description = dto.Description,
                Images = imagesPath, // Store comma-separated paths
                Condition = dto.Condition,
                Location = dto.Location,
                BasePrice = dto.BasePrice,
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };

            var createdItem = await _itemRepository.CreateAsync(item);

            // Reload with includes to get full data
            return await GetByIdAsync(createdItem.Id);
        }
        private static ItemResponseDto MapToResponseDto(Item item)
        {
            // Lấy auction mới nhất của item (ưu tiên active, sau đó lấy bất kỳ)
            // Để hiển thị tất cả auctions, không chỉ active
            var activeAuction = item.Auctions?
                .OrderByDescending(a => a.Status == "active" ? 1 : 0) // Ưu tiên active trước
                .ThenByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            return new ItemResponseDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                BasePrice = item.BasePrice,
                Condition = item.Condition,
                Images = item.Images,
                Location = item.Location,
                Status = item.Status,
                CreatedAt = item.CreatedAt,

                // Category Info
                CategoryId = item.CategoryId,
                CategoryName = item.Category?.Name,
                CategorySlug = item.Category?.Slug,
                CategoryIcon = item.Category?.Icon,

                // Seller Info
                SellerId = item.SellerId,
                SellerName = item.Seller?.FullName,
                SellerEmail = item.Seller?.Email,
                SellerAvatar = item.Seller?.AvatarUrl,
                SellerReputationScore = item.Seller?.ReputationScore,
                SellerTotalSales = item.Seller?.TotalSales,

                // Auction Info (NEW)
                AuctionId = activeAuction?.Id,
                StartingBid = activeAuction?.StartingBid,
                CurrentBid = activeAuction?.CurrentBid,
                BidCount = activeAuction?.BidCount,
                AuctionEndTime = activeAuction?.EndTime,
                AuctionStatus = activeAuction?.Status
            };
        }
    }
}
