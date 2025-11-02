namespace BitNow_Backend.DTOs
{
    public class ItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Images { get; set; } 
        public CategoryItemDto? Category { get; set; }
        public UserSellerDto? Seller { get; set; }
    }
}