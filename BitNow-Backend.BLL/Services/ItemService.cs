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
                ItemSpecifics = dto.ItemSpecifics,
                Images = imagesPath, // Store comma-separated paths
                Condition = dto.Condition,
                Location = dto.Location,
                BasePrice = dto.BasePrice,
                Status = "pending",
                CreatedAt = DateTime.Now
            };

            var createdItem = await _itemRepository.CreateAsync(item);

            // Reload with includes to get full data
            return await GetByIdAsync(createdItem.Id);
        }

        public async Task<ItemResponseDto?> CreateDraftItemAsync(CreateItemDto dto, string? imagesPath = null)
        {
            // Validate required fields (less strict for draft)
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new ArgumentException("Title is required");
            }

            // Check if category exists
            var categories = await _itemRepository.GetCategoriesAsync();
            if (!categories.Any(c => c.Id == dto.CategoryId))
            {
                throw new ArgumentException("Category not found");
            }

            // Create item with draft status
            var item = new Item
            {
                SellerId = dto.SellerId,
                CategoryId = dto.CategoryId,
                Title = dto.Title,
                Description = dto.Description,
                ItemSpecifics = dto.ItemSpecifics,
                Images = imagesPath, // Store comma-separated paths
                Condition = dto.Condition,
                Location = dto.Location,
                BasePrice = dto.BasePrice > 0 ? dto.BasePrice : 0, // Allow 0 for draft
                Status = "draft",
                CreatedAt = DateTime.Now
            };

            var createdItem = await _itemRepository.CreateAsync(item);

            // Reload with includes to get full data
            return await GetByIdAsync(createdItem.Id);
        }

        public async Task<ItemResponseDto?> UpdateDraftItemAsync(int id, CreateItemDto dto, string? imagesPath = null)
        {
            // Get existing item
            var existingItem = await _itemRepository.GetByIdAsync(id);
            if (existingItem == null)
            {
                return null;
            }

            // Only allow updating draft items
            if (existingItem.Status?.ToLower() != "draft")
            {
                throw new InvalidOperationException("Chỉ có thể cập nhật các sản phẩm ở trạng thái bản nháp");
            }

            // Update item properties
            existingItem.Title = dto.Title;
            existingItem.Description = dto.Description;
            existingItem.ItemSpecifics = dto.ItemSpecifics;
            existingItem.CategoryId = dto.CategoryId;
            existingItem.BasePrice = dto.BasePrice;
            existingItem.Condition = dto.Condition;
            existingItem.Location = dto.Location;
            
            // Update images if provided
            if (!string.IsNullOrWhiteSpace(imagesPath))
            {
                existingItem.Images = imagesPath;
            }

            // Keep Status as "draft" and CreatedAt unchanged

            var updatedItem = await _itemRepository.UpdateAsync(existingItem);

            // Reload with includes to get full data
            return await GetByIdAsync(updatedItem.Id);
        }

        private static ItemResponseDto MapToResponseDto(Item item)
        {
            // Chỉ lấy các auction còn hiệu lực cho người mua:
            // - Không lấy các auction đã hủy (cancelled) hoặc đã hoàn tất (completed)
            // - Không lấy các auction đã hết hạn (EndTime <= now)
            // => Giữ lại: active/scheduled/paused còn hiệu lực
            var now = DateTime.Now;

            var visibleAuctions = item.Auctions?
                .Where(a =>
                    a.Status != null &&
                    !a.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) &&
                    !a.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) &&
                    a.EndTime > now);

            // Ưu tiên auction đang active, sau đó paused, sau đó các trạng thái khác (nếu có)
            var activeAuction = visibleAuctions?
                .OrderByDescending(a =>
                    string.Equals(a.Status, "active", StringComparison.OrdinalIgnoreCase) ? 2 :
                    string.Equals(a.Status, "paused", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            return new ItemResponseDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ItemSpecifics = item.ItemSpecifics,
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
                AuctionStartTime = activeAuction?.StartTime,
                AuctionEndTime = activeAuction?.EndTime,
                AuctionStatus = activeAuction?.Status
            };
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            return await _itemRepository.DeleteAsync(id);
        }
    }
}
