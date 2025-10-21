using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class ContactMessage
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Status { get; set; }

    public string? AdminReply { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}


