using System;
using System.Collections.Generic;

namespace BitNow_Backend.DAL.Models;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}


