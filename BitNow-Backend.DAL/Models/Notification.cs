using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Type { get; set; }

    public string Message { get; set; } = null!;

    public string? Link { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}


