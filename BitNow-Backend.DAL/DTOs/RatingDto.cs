using System.ComponentModel.DataAnnotations;

namespace BitNow_Backend.DAL.DTOs;

public class RatingCreateDto
{
    [Required]
    public int AuctionId { get; set; }

    [Required]
    public int RaterId { get; set; }

    [Required]
    public int RatedId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}

public class RatingResponseDto
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public int RaterId { get; set; }
    public int RatedId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime? CreatedAt { get; set; }
}


