using System;
using System.Collections.Generic;

namespace CCUTrade.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Department { get; set; } = "";
    public string CourseName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsSold { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string SellerName { get; set; } = "";
    public string ContactInfo { get; set; } = "";
    public string Location { get; set; } = "";
    public string CampusSection { get; set; } = "一般商品";
    public int ReviewRating { get; set; } = 0;
    public string ReviewComment { get; set; } = "";
    public DateTime? ReviewedAt { get; set; }
    public string VerifiedSchoolEmail { get; set; } = "";
    public byte[]? PhotoData { get; set; }

    // 🌟 核心升級：移除 NotMapped！改成實體的導覽屬性（Navigation Property）
    // 這樣 EF Core 才會在 SQL Server 幫我們把留言關聯表建立起來
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}