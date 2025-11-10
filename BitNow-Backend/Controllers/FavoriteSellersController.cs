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
    [Authorize] // Yêu cầu đăng nhập
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
            if (buyerId == null) return Unauthorized();

            var favorites = await _favoriteSellerService.GetFavoritesAsync(buyerId.Value);
            return Ok(favorites);
        }

        // GET: api/FavoriteSellers/check/{sellerId}
        [HttpGet("check/{sellerId}")]
        public async Task<ActionResult<bool>> CheckIsFavorite(int sellerId)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null) return Unauthorized();

            var isFavorite = await _favoriteSellerService.IsFavoriteAsync(buyerId.Value, sellerId);
            return Ok(new { isFavorite });
        }

        // POST: api/FavoriteSellers
        [HttpPost]
        public async Task<ActionResult<FavoriteSellerResponseDto>> AddFavorite([FromBody] AddFavoriteSellerDto dto)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null) return Unauthorized();

            var result = await _favoriteSellerService.AddFavoriteAsync(buyerId.Value, dto.SellerId);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // DELETE: api/FavoriteSellers/{sellerId}
        [HttpDelete("{sellerId}")]
        public async Task<ActionResult<FavoriteSellerResponseDto>> RemoveFavorite(int sellerId)
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == null) return Unauthorized();

            var result = await _favoriteSellerService.RemoveFavoriteAsync(buyerId.Value, sellerId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        // Helper method to get current user ID from JWT token
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            return null;
        }
    }
}
