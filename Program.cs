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
}