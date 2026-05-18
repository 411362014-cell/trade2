using System;

namespace CCUTrade.Models;

public class WishlistItem
{
    public int Id { get; set; }

    public string Keyword { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}