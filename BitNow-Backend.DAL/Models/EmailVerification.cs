using System;

namespace BitNow_Backend.DAL.Models;

public class EmailVerification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public virtual User User { get; set; } = null!;
}


