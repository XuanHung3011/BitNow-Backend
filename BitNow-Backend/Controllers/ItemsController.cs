using BitNow_Backend.DAL.IRepositories;
using BitNow_Backend.DAL.Models;
using BitNow_Backend.DTOs; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BitNow_Backend.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IItemRepository _itemRepository;
        private const int DefaultPageSize = 10;

        public ItemsController(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        private ItemDto MapToItemDto(Item item)
        {
            return new ItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                BasePrice = item.BasePrice,
                Condition = item.Condition,
                Location = item.Location,
                Status = item.Status,
                CreatedAt = (DateTime)item.CreatedAt,
                Images = item.Images,

                Category = item.Category != null ? new CategoryItemDto
                {
                    Id = item.Category.Id,
                    Name = item.Category.Name,
                    Slug = item.Category.Slug,
                    Icon = item.Category.Icon
                } : null,

                Seller = item.Seller != null ? new UserSellerDto
                {
                    Id = item.Seller.Id,
                    FullName = item.Seller.FullName,
                    Email = item.Seller.Email,
                    ReputationScore = (decimal)item.Seller.ReputationScore
                } : null
            };
        }

        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> GetItems([FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            if (page < 1) return BadRequest("Page number must be 1 or greater.");
            if (pageSize < 1) pageSize = DefaultPageSize;

            var items = await _itemRepository.GetPagedAsync(page, pageSize);
            var totalCount = await _itemRepository.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var itemDtos = items.Select(MapToItemDto).ToList();

            return Ok(new
            {
                TotalItems = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = itemDtos 
            });
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetItem(int id)
        {
            var item = await _itemRepository.GetByIdAsync(id);

            if (item == null)
            {
                return NotFound($"Item with ID {id} not found.");
            }

            var itemDto = MapToItemDto(item);
            return Ok(itemDto);
        }

        [HttpGet("BySeller/{sellerId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetItemsBySeller(int sellerId, [FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        {
            if (sellerId <= 0) return BadRequest("Invalid Seller ID.");
            if (page < 1) return BadRequest("Page number must be 1 or greater.");
            if (pageSize < 1) pageSize = DefaultPageSize;

            var items = await _itemRepository.GetBySellerIdAsync(sellerId, page, pageSize);
            var totalCount = await _itemRepository.CountBySellerIdAsync(sellerId);
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var itemDtos = items.Select(MapToItemDto).ToList();

            return Ok(new
            {
                SellerId = sellerId,
                TotalItems = totalCount,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = itemDtos 
            });
        }



        [HttpPost]
        [ProducesResponseType(201)] 
        [ProducesResponseType(400)] 
        public async Task<IActionResult> CreateItem([FromBody] CreateItemRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newItem = new Item
            {
                Title = requestDto.Title,
                Description = requestDto.Description,
                CategoryId = requestDto.CategoryId,
                BasePrice = requestDto.BasePrice,
                Condition = requestDto.Condition,
                Location = requestDto.Location,
                Images = requestDto.Images,
                SellerId = requestDto.SellerId,
                Status = "pending", 
                CreatedAt = DateTime.UtcNow 
            };

            var newAuction = new Auction
            {
                SellerId = requestDto.SellerId,
                StartingBid = requestDto.StartingBid,
                CurrentBid = requestDto.StartingBid, 
                BuyNowPrice = requestDto.BuyNowPrice,
                StartTime = DateTime.UtcNow,
                EndTime = requestDto.EndTime,
                Status = "active", 
                BidCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var createdItem = await _itemRepository.AddAsync(newItem, newAuction);

                var itemDto = MapToItemDto(createdItem);

                return CreatedAtAction(nameof(GetItem), new { id = itemDto.Id }, itemDto);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest("Database error occurred: " + ex.InnerException?.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }


    }
}