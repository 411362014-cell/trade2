using System;

namespace CCUTrade.Models;

public class UserAccount
{
    public int Id { get; set; }

    public string StudentName { get; set; } = "";

    public string SchoolEmail { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}