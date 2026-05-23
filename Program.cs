using Avalonia;
using System;
using CCUTrade.Data;

namespace CCUTrade;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    // 🌟 最終加固：為了前台社群卡片排版特別外掛的純顯示欄位（[NotMapped] 確保不會影響到現有的資料庫建表）
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DaysLeftText { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string StatusText { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string StatusColor { get; set; } = "";

}

