using BitNow_Backend.BLL.IServices;
using BitNow_Backend.DAL.DTOs;
using BitNow_Backend.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BitNow_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class FavoriteSellersController : ControllerBase
    {
        private readonly IFavoriteSellerService _favoriteSellerService;

        public FavoriteSellersController(IFavoriteSellerService favoriteSellerService)
        {
            _favoriteSellerService = favoriteSellerService;
        }

        // GET: api/FavoriteSellers
        [HttpGet]
        public async Task<ActionResult<List<FavoriteSellerDto>>> GetMyFavorites()
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null)
                return Unauthorized(new { message = "Vui lòng đăng nhập" });

            var favorites = await _favoriteSellerService.GetFavoritesAsync(buyerId.Value);
            return Ok(favorites);
        }

        // GET: api/FavoriteSellers/check/{sellerId}
        [HttpGet("check/{sellerId}")]
        public async Task<ActionResult<object>> CheckIsFavorite(int sellerId)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null)
                return Ok(new { isFavorite = false }); // Trả về false thay vì Unauthorized

            var isFavorite = await _favoriteSellerService.IsFavoriteAsync(buyerId.Value, sellerId);
            return Ok(new { isFavorite });
        }

        [HttpPost]
        public async Task<ActionResult<FavoriteSellerResponseDto>> AddFavorite([FromBody] AddFavoriteSellerDto dto)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null)
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });

            var result = await _favoriteSellerService.AddFavoriteAsync(buyerId.Value, dto.SellerId);

            // Debug log
            Console.WriteLine($"AddFavorite result: Success={result.Success}, Message={result.Message}");

            if (!result.Success)
                return BadRequest(result);

            return Ok(result); // Đảm bảo trả về 200 OK với result
        }

        // DELETE: api/FavoriteSellers/{sellerId}
        [HttpDelete("{sellerId}")]
        public async Task<ActionResult<FavoriteSellerResponseDto>> RemoveFavorite(int sellerId)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null)
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });

            var result = await _favoriteSellerService.RemoveFavoriteAsync(buyerId.Value, sellerId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // Helper method to get current user ID from header (thay vì JWT)
        private int? GetCurrentUserId()
        {
            // Lấy userId từ header X-User-Id
            var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(userIdHeader) && int.TryParse(userIdHeader, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}
