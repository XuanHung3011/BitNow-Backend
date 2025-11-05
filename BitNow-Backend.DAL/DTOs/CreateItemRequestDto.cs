using System.ComponentModel.DataAnnotations;

namespace BitNow_Backend.DAL.DTOs
{
    public class CreateItemRequestDto
    {
        // Item Fields
        [Required]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Category ID must be valid.")]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Base Price must be greater than 0.")]
        public decimal BasePrice { get; set; }

        [Required]
        [StringLength(50)]
        public string Condition { get; set; } = string.Empty;

        [StringLength(255)]
        public string Location { get; set; } = string.Empty;
        public string? Images { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Starting Bid must be greater than 0.")]
        public decimal StartingBid { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public decimal? BuyNowPrice { get; set; }
    }
}