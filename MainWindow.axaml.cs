using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CCUTrade.Data;
using CCUTrade.Models;

namespace CCUTrade;

// 🌟 貼文卡片專屬包裝盒：包裝前端動態顯示欄位
public class ProductPostViewModel
{
    public Product SourceProduct { get; set; } = new();
    public int Id => SourceProduct.Id;
    public string Name => SourceProduct.Name;
    public decimal Price => SourceProduct.Price;
    public string Description => SourceProduct.Description ?? "";
    public string Department => SourceProduct.Department ?? "未填寫";
    public string CourseName => SourceProduct.CourseName ?? "無";
    public string Category => SourceProduct.Category ?? "其他";
    public string CampusSection => SourceProduct.CampusSection ?? "一般商品";
    public string SellerName => SourceProduct.SellerName ?? "";
    public string ContactInfo => SourceProduct.ContactInfo ?? "";
    public string VerifiedSchoolEmail => SourceProduct.VerifiedSchoolEmail ?? "";

    // 前端專用動態渲染欄位
    public string DaysLeftText { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string StatusColor { get; set; } = "";
}

public partial class MainWindow : Window
{
    private List<Product> _currentProducts = new();
    private List<WishlistItem> _currentWishlistItems = new();
    private List<string> _bellNotifications = new(); // 儲存小鈴鐺的匹配成功紀錄

    private string _currentUserName = "";
    private string _currentUserEmail = "";

    public MainWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        // 全量控制項手動點名綁定
        CategoryListBox = this.FindControl<ComboBox>("CategoryListBox");
        CampusSectionListBox = this.FindControl<ComboBox>("CampusSectionListBox");
        FilterCategoryListBox = this.FindControl<ComboBox>("FilterCategoryListBox");
        CampusSectionFilterListBox = this.FindControl<ComboBox>("CampusSectionFilterListBox");
        StatusFilterListBox = this.FindControl<ComboBox>("StatusFilterListBox");
        ReviewRatingListBox = this.FindControl<ComboBox>("ReviewRatingListBox");

        ProductList = this.FindControl<ListBox>("ProductList");
        GraduationProductList = this.FindControl<ListBox>("GraduationProductList");
        SellerManagementPlainListBox = this.FindControl<ListBox>("SellerManagementPlainListBox");
        WishlistList = this.FindControl<ListBox>("WishlistList");
        RecommendedMeetingLocationListBox = this.FindControl<ListBox>("RecommendedMeetingLocationListBox");
        BellNotificationListBox = this.FindControl<ListBox>("BellNotificationListBox");

        StudentNameTextBox = this.FindControl<TextBox>("StudentNameTextBox");
        SchoolEmailTextBox = this.FindControl<TextBox>("SchoolEmailTextBox");
        NameTextBox = this.FindControl<TextBox>("NameTextBox");
        DescriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        DepartmentTextBox = this.FindControl<TextBox>("DepartmentTextBox");
        CourseNameTextBox = this.FindControl<TextBox>("CourseNameTextBox");
        PriceTextBox = this.FindControl<TextBox>("PriceTextBox");
        SellerNameTextBox = this.FindControl<TextBox>("SellerNameTextBox");
        ContactInfoTextBox = this.FindControl<TextBox>("ContactInfoTextBox");
        LocationTextBox = this.FindControl<TextBox>("LocationTextBox");
        WishlistKeywordTextBox = this.FindControl<TextBox>("WishlistKeywordTextBox");
        ReviewCommentTextBox = this.FindControl<TextBox>("ReviewCommentTextBox");
        SearchTextBox = this.FindControl<TextBox>("SearchTextBox");
        DepartmentSearchTextBox = this.FindControl<TextBox>("DepartmentSearchTextBox");
        CourseSearchTextBox = this.FindControl<TextBox>("CourseSearchTextBox");
        MinPriceTextBox = this.FindControl<TextBox>("MinPriceTextBox");
        MaxPriceTextBox = this.FindControl<TextBox>("MaxPriceTextBox");

        NotificationTextBlock = this.FindControl<TextBlock>("NotificationTextBlock");
        CurrentUserTextBlock = this.FindControl<TextBlock>("CurrentUserTextBlock");
        LoginOverlayPage = this.FindControl<Grid>("LoginOverlayPage");
        MainWebsiteTabControl = this.FindControl<TabControl>("MainWebsiteTabControl");
        BellNotificationPanel = this.FindControl<Border>("BellNotificationPanel");
        CloseBellPanelButton = this.FindControl<Button>("CloseBellPanelButton");

        if (CategoryListBox != null) CategoryListBox.SelectedIndex = 0;
        if (CampusSectionListBox != null) CampusSectionListBox.SelectedIndex = 0;
        if (FilterCategoryListBox != null) FilterCategoryListBox.SelectedIndex = 0;
        if (CampusSectionFilterListBox != null) CampusSectionFilterListBox.SelectedIndex = 0;
        if (StatusFilterListBox != null) StatusFilterListBox.SelectedIndex = 0;
        if (ReviewRatingListBox != null) ReviewRatingListBox.SelectedIndex = 0;

        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        RefreshProducts();
        RefreshWishlist();
    }

    // 🌟 中正信箱經典認證解封機制
    private void RegisterOrLoginButton_Click(object? sender, RoutedEventArgs e)
    {
        if (StudentNameTextBox == null || SchoolEmailTextBox == null || NotificationTextBlock == null || CurrentUserTextBlock == null || LoginOverlayPage == null) return;

        var studentName = StudentNameTextBox.Text?.Trim() ?? "";
        var schoolEmail = SchoolEmailTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(schoolEmail))
        {
            NotificationTextBlock.Text = "⚠️ 請完整輸入姓名與學校 Email！";
            return;
        }

        if (!IsSchoolEmailValid(schoolEmail))
        {
            NotificationTextBlock.Text = "⚠️ 請使用中正大學校內 Email 認證，例如：abc123@ccu.edu.tw 或 @alum.ccu.edu.tw";
            return;
        }

        using var db = new AppDbContext();
        var existingUser = db.UserAccounts.FirstOrDefault(u => u.SchoolEmail == schoolEmail);

        if (existingUser == null)
        {
            db.UserAccounts.Add(new UserAccount { StudentName = studentName, SchoolEmail = schoolEmail, CreatedAt = DateTime.Now });
        }
        else
        {
            existingUser.StudentName = studentName;
        }
        db.SaveChanges();

        _currentUserName = studentName;
        _currentUserEmail = schoolEmail;

        CurrentUserTextBlock.Text = $"目前登入帳號：{_currentUserName}（{_currentUserEmail}）";
        NotificationTextBlock.Text = $"🎉 已完成校內帳號認證：{_currentUserName}";

        // 移開遮罩牆
        LoginOverlayPage.IsVisible = false;
        RefreshProducts();
    }

    private bool IsSchoolEmailValid(string email)
    {
        return email.EndsWith("@ccu.edu.tw", StringComparison.OrdinalIgnoreCase) ||
               email.EndsWith("@alum.ccu.edu.tw", StringComparison.OrdinalIgnoreCase);
    }

    private void AddProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || NameTextBox == null || PriceTextBox == null || SellerNameTextBox == null) return;

        if (string.IsNullOrWhiteSpace(_currentUserEmail))
        {
            NotificationTextBlock.Text = "⚠️ 請先完成校內帳號認證，才能新增商品";
            return;
        }

        var name = NameTextBox.Text?.Trim() ?? "";
        var priceText = PriceTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name) || !decimal.TryParse(priceText, out decimal price))
        {
            NotificationTextBlock.Text = "⚠️ 請填寫正確的商品名稱與價格";
            return;
        }

        var product = new Product
        {
            Name = name,
            Price = price,
            Description = DescriptionTextBox?.Text?.Trim() ?? "",
            Department = DepartmentTextBox?.Text?.Trim() ?? "未填寫",
            CourseName = CourseNameTextBox?.Text?.Trim() ?? "無",
            Category = GetListBoxText(CategoryListBox, "其他"),
            CampusSection = GetListBoxText(CampusSectionListBox, "一般商品"),
            SellerName = SellerNameTextBox.Text?.Trim() ?? "",
            ContactInfo = ContactInfoTextBox?.Text?.Trim() ?? "",
            Location = LocationTextBox?.Text?.Trim() ?? "校內面交",
            VerifiedSchoolEmail = _currentUserEmail,
            CreatedAt = DateTime.Now,
            IsSold = false
        };

        using var db = new AppDbContext();
        db.Products.Add(product);
        db.SaveChanges();

        ClearInputFields();
        RefreshProducts();
        CheckWishlistNotification(product);
    }

    private void MarkAsSoldButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null) return;
        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "⚠️ 請先選取歷史清單中欲標記售出的商品";
            return;
        }

        using var db = new AppDbContext();
        var product = db.Products.FirstOrDefault(p => p.Id == selectedProduct.Id);

        if (product != null)
        {
            product.IsSold = true;
            db.SaveChanges();
            RefreshProducts();
            NotificationTextBlock.Text = $"已將商品標記為已售出。🤝 請填寫本次交易的滿意度評價！";
        }
    }

    private void RefreshProducts()
    {
        if (ProductList == null || GraduationProductList == null || SellerManagementPlainListBox == null) return;

        using var db = new AppDbContext();
        var allProducts = db.Products.OrderByDescending(p => p.CreatedAt).Where(p => p.CreatedAt >= DateTime.Now.AddDays(-30)).ToList();
        _currentProducts = allProducts;

        var postViewModels = allProducts.Select(p => {
            var daysLeft = 30 - (DateTime.Now.Date - p.CreatedAt.Date).Days;
            return new ProductPostViewModel
            {
                SourceProduct = p,
                DaysLeftText = $"⏳ 剩餘 {Math.Max(0, daysLeft)} 天",
                StatusText = p.IsSold ? "已售出 🤝" : "販售中 🔥",
                StatusColor = p.IsSold ? "#718096" : "#27AE60"
            };
        }).ToList();

        ProductList.ItemsSource = postViewModels;
        GraduationProductList.ItemsSource = postViewModels.Where(v => v.CampusSection == "畢業傳承商品").ToList();

        var plainDisplayList = postViewModels.Select(v => $"{v.Name} | ${v.Price} | {v.StatusText}").ToList();
        SellerManagementPlainListBox.ItemsSource = plainDisplayList;
    }

    private void CheckWishlistNotification(Product product)
    {
        using var db = new AppDbContext();
        var wishlistItems = db.WishlistItems.ToList();
        var matched = wishlistItems.Where(w => product.Name.Contains(w.Keyword) || (product.Description != null && product.Description.Contains(w.Keyword))).Select(w => w.Keyword).ToList();

        if (matched.Count > 0)
        {
            _bellNotifications.Insert(0, $"[{DateTime.Now:HH:mm}] 🎉 許願匹配：大廳出現了符合您追蹤「{string.Join("、", matched)}」的物資：{product.Name}！");
            TriggerBellUI();
        }
    }

    private void AddWishlistButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WishlistKeywordTextBox == null || NotificationTextBlock == null) return;
        var kw = WishlistKeywordTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(kw)) return;

        using var db = new AppDbContext();
        if (!db.WishlistItems.Any(w => w.Keyword == kw))
        {
            db.WishlistItems.Add(new WishlistItem { Keyword = kw, CreatedAt = DateTime.Now });
            db.SaveChanges();
            NotificationTextBlock.Text = $"已將關鍵字「{kw}」加入智慧追蹤！";

            var matchedOldProducts = db.Products.Where(p => p.Name.Contains(kw) || (p.Description != null && p.Description.Contains(kw))).ToList();
            foreach (var p in matchedOldProducts)
            {
                _bellNotifications.Insert(0, $"[{DateTime.Now:HH:mm}] 🔮 匹配成功：資料庫已有商品：{p.Name} (${p.Price})！");
            }
            if (matchedOldProducts.Count > 0) TriggerBellUI();
        }
        WishlistKeywordTextBox.Text = "";
        RefreshWishlist();
    }

    private void TriggerBellUI()
    {
        if (BellNotificationListBox != null) { BellNotificationListBox.ItemsSource = null; BellNotificationListBox.ItemsSource = _bellNotifications; }
        if (NotificationTextBlock != null) NotificationTextBlock.Text = $"🔔 右上角智慧小鈴鐺響起！您許願的商品有更新了！";
    }

    private void NotificationBellButton_Click(object? sender, RoutedEventArgs e)
    {
        if (BellNotificationPanel == null || CloseBellPanelButton == null) return;
        BellNotificationPanel.IsVisible = true;
        CloseBellPanelButton.IsVisible = true;
        if (_bellNotifications.Count == 0 && BellNotificationListBox != null) BellNotificationListBox.ItemsSource = new List<string> { "目前尚未有智慧通知。" };
    }

    private void CloseBellPanelButton_Click(object? sender, RoutedEventArgs e)
    {
        if (BellNotificationPanel != null) BellNotificationPanel.IsVisible = false;
        if (CloseBellPanelButton != null) CloseBellPanelButton.IsVisible = false;
    }

    private void DeleteWishlistButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WishlistList == null || WishlistList.SelectedIndex < 0) return;
        using var db = new AppDbContext();
        var items = db.WishlistItems.OrderByDescending(w => w.CreatedAt).ToList();
        if (WishlistList.SelectedIndex < items.Count) { db.WishlistItems.Remove(items[WishlistList.SelectedIndex]); db.SaveChanges(); }
        RefreshWishlist();
    }

    private void RefreshWishlist()
    {
        if (WishlistList == null) return;
        using var db = new AppDbContext();
        _currentWishlistItems = db.WishlistItems.OrderByDescending(w => w.CreatedAt).ToList();
        var list = _currentWishlistItems.Select(w => $"🔮 監控關鍵字：{w.Keyword}").ToList();
        WishlistList.ItemsSource = list.Count == 0 ? new List<string> { "清單是空的，快去大廳許願吧！" } : list;
    }

    private void OnGoToHomePage_Click(object? sender, RoutedEventArgs e) { if (MainWebsiteTabControl != null) MainWebsiteTabControl.SelectedIndex = 0; }
    private void OnGoToGraduationPage_Click(object? sender, RoutedEventArgs e) { if (MainWebsiteTabControl != null) MainWebsiteTabControl.SelectedIndex = 1; }
    private void OnGoToSellerPage_Click(object? sender, RoutedEventArgs e) { if (MainWebsiteTabControl != null) MainWebsiteTabControl.SelectedIndex = 2; }

    private void SearchTextBox_TextChanged(object? sender, TextChangedEventArgs e) => RefreshProducts();
    private void DepartmentSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e) => RefreshProducts();
    private void CourseSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e) => RefreshProducts();
    private void PriceRangeTextBox_TextChanged(object? sender, TextChangedEventArgs e) => RefreshProducts();
    private void FilterCategoryListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) => RefreshProducts();
    private void CampusSectionFilterListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) => RefreshProducts();
    private void StatusFilterListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) => RefreshProducts();
    private void RefreshButton_Click(object? sender, RoutedEventArgs e) => RefreshProducts();

    private void ClearSearchButton_Click(object? sender, RoutedEventArgs e)
    {
        if (SearchTextBox != null) SearchTextBox.Text = "";
        if (MinPriceTextBox != null) MinPriceTextBox.Text = "";
        if (MaxPriceTextBox != null) MaxPriceTextBox.Text = "";
        RefreshProducts();
    }

    private void LoadSelectedProductButton_Click(object? sender, RoutedEventArgs e)
    {
        var prod = GetSelectedProduct();
        if (prod == null || NameTextBox == null || PriceTextBox == null) return;
        NameTextBox.Text = prod.Name;
        PriceTextBox.Text = prod.Price.ToString();
        if (DescriptionTextBox != null) DescriptionTextBox.Text = prod.Description;
        if (DepartmentTextBox != null) DepartmentTextBox.Text = prod.Department;
        if (CourseNameTextBox != null) CourseNameTextBox.Text = prod.CourseName;
        if (SellerNameTextBox != null) SellerNameTextBox.Text = prod.SellerName;
        if (ContactInfoTextBox != null) ContactInfoTextBox.Text = prod.ContactInfo;
        if (LocationTextBox != null) LocationTextBox.Text = prod.Location;
    }

    private void UpdateProductButton_Click(object? sender, RoutedEventArgs e)
    {
        var prod = GetSelectedProduct();
        if (prod == null || NameTextBox == null) return;
        using var db = new AppDbContext();
        var target = db.Products.FirstOrDefault(p => p.Id == prod.Id);
        if (target != null)
        {
            target.Name = NameTextBox.Text?.Trim() ?? "";
            if (decimal.TryParse(PriceTextBox?.Text, out decimal pr)) target.Price = pr;
            target.Description = DescriptionTextBox?.Text?.Trim() ?? "";
            db.SaveChanges();
            ClearInputFields();
            RefreshProducts();
        }
    }

    private void DeleteProductButton_Click(object? sender, RoutedEventArgs e)
    {
        var prod = GetSelectedProduct();
        if (prod == null) return;
        using var db = new AppDbContext();
        var target = db.Products.FirstOrDefault(p => p.Id == prod.Id);
        if (target != null) { db.Products.Remove(target); db.SaveChanges(); }
        ClearInputFields();
        RefreshProducts();
    }

    private void LoadReviewButton_Click(object? sender, RoutedEventArgs e)
    {
        var prod = GetSelectedProduct();
        if (prod == null || ReviewCommentTextBox == null) return;
        ReviewCommentTextBox.Text = prod.ReviewComment ?? "買家交易完成，面交過程非常順暢！";
    }

    private void SaveReviewButton_Click(object? sender, RoutedEventArgs e)
    {
        var prod = GetSelectedProduct();
        if (prod == null || ReviewCommentTextBox == null) return;
        using var db = new AppDbContext();
        var target = db.Products.FirstOrDefault(p => p.Id == prod.Id);
        if (target != null)
        {
            target.ReviewComment = ReviewCommentTextBox.Text;
            target.ReviewRating = 5;
            db.SaveChanges();
            ReviewCommentTextBox.Text = "";
            RefreshProducts();
            if (NotificationTextBlock != null) NotificationTextBlock.Text = "🎉 交易評價儲存成功！";
        }
    }

    private Product? GetSelectedProduct()
    {
        if (SellerManagementPlainListBox == null || SellerManagementPlainListBox.SelectedIndex < 0) return null;
        if (SellerManagementPlainListBox.SelectedIndex >= _currentProducts.Count) return null;
        return _currentProducts[SellerManagementPlainListBox.SelectedIndex];
    }

    private string GetListBoxText(Control? box, string def)
    {
        if (box is ComboBox cb && cb.SelectedItem is ListBoxItem lbi) return lbi.Content?.ToString() ?? def;
        return def;
    }

    private void RecommendedMeetingLocationListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LocationTextBox == null) return;
        var loc = GetListBoxText(RecommendedMeetingLocationListBox, "");
        if (!string.IsNullOrWhiteSpace(loc)) LocationTextBox.Text = loc;
    }

    private void ClearInputFields()
    {
        if (NameTextBox != null) NameTextBox.Text = "";
        if (PriceTextBox != null) PriceTextBox.Text = "";
        if (DescriptionTextBox != null) DescriptionTextBox.Text = "";
    }
}