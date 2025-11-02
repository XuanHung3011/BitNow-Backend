namespace BitNow_Backend.DTOs
{
    public class UserSellerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ReputationScore { get; set; }
    }
}