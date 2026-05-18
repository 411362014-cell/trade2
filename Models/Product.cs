using System;

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
}