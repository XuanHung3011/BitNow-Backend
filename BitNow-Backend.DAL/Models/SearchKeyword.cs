using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public class SearchKeyword
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Keyword { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}



