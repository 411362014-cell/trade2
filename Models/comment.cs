using System;

namespace CCUTrade.Models;

public class Comment
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}