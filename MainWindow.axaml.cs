using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CCUTrade.Data;
using CCUTrade.Models;

namespace CCUTrade;

// 🌟 貼文卡片專屬包裝盒：全面注入 INotifyPropertyChanged 屬性通知，確保留言區展開、新留言跳出時前端立刻連動！
public class ProductPostViewModel : INotifyPropertyChanged
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
    public string Location => SourceProduct.Location ?? "校內面交";

    public string DaysLeftText { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string StatusColor { get; set; } = "";

    // 🌟 專屬留言艙擴充
    private bool _isCommentVisible = false;
    public bool IsCommentVisible
    {
        get => _isCommentVisible;
        set { _isCommentVisible = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> Comments { get; set; } = new()
    {
        "💡 系統提示：目前此求購/物資暫無留言，快來搶沙發詢問吧！"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class SellerReviewDisplayItem
{
    public string Name { get; set; } = "";
    public string ReviewRatingText { get; set; } = "";
    public string ReviewComment { get; set; } = "";
}

public partial class MainWindow : Window
{
    private List<Product> _currentProducts = new();
    private List<WishlistItem> _currentWishlistItems = new();
    private ObservableCollection<string> _bellNotifications = new ObservableCollection<string>();

    private string _currentUserName = "匿名同學";
    private string _currentUserEmail = "";

    public MainWindow()
    {
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        // 全量控制項點名
        CategoryListBox = this.FindControl<ComboBox>("CategoryListBox");
        CampusSectionListBox = this.FindControl<ComboBox>("CampusSectionListBox");
        FilterCategoryListBox = this.FindControl<ComboBox>("FilterCategoryListBox");
        CampusSectionFilterListBox = this.FindControl<ComboBox>("CampusSectionFilterListBox");
        StatusFilterListBox = this.FindControl<ComboBox>("StatusFilterListBox");
        ReviewRatingListBox = this.FindControl<ComboBox>("ReviewRatingListBox");
        BuyerWishCategoryListBox = this.FindControl<ComboBox>("BuyerWishCategoryListBox");

        ProductList = this.FindControl<ListBox>("ProductList");
        GraduationProductList = this.FindControl<ListBox>("GraduationProductList");
        BuyerWishProductList = this.FindControl<ListBox>("BuyerWishProductList");
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

        BuyerWishNameTextBox = this.FindControl<TextBox>("BuyerWishNameTextBox");
        BuyerWishPriceTextBox = this.FindControl<TextBox>("BuyerWishPriceTextBox");
        BuyerWishDepartmentTextBox = this.FindControl<TextBox>("BuyerWishDepartmentTextBox");
        BuyerWishCourseNameTextBox = this.FindControl<TextBox>("BuyerWishCourseNameTextBox");
        BuyerWishSellerNameTextBox = this.FindControl<TextBox>("BuyerWishSellerNameTextBox");
        BuyerWishContactInfoTextBox = this.FindControl<TextBox>("BuyerWishContactInfoTextBox");
        BuyerWishLocationTextBox = this.FindControl<TextBox>("BuyerWishLocationTextBox");
        BuyerWishDescriptionTextBox = this.FindControl<TextBox>("BuyerWishDescriptionTextBox");

        NotificationTextBlock = this.FindControl<TextBlock>("NotificationTextBlock");
        CurrentUserTextBlock = this.FindControl<TextBlock>("CurrentUserTextBlock");
        LoginOverlayPage = this.FindControl<Grid>("LoginOverlayPage");
        MainWebsiteTabControl = this.FindControl<TabControl>("MainWebsiteTabControl");
        BellNotificationPanel = this.FindControl<Border>("BellNotificationPanel");
        CloseBellPanelButton = this.FindControl<Button>("CloseBellPanelButton");

        if (BellNotificationListBox != null) BellNotificationListBox.ItemsSource = _bellNotifications;

        if (CategoryListBox != null) CategoryListBox.SelectedIndex = 0;
        if (CampusSectionListBox != null) CampusSectionListBox.SelectedIndex = 0;
        if (FilterCategoryListBox != null) FilterCategoryListBox.SelectedIndex = 0;
        if (CampusSectionFilterListBox != null) CampusSectionFilterListBox.SelectedIndex = 0;
        if (StatusFilterListBox != null) StatusFilterListBox.SelectedIndex = 0;
        if (ReviewRatingListBox != null) ReviewRatingListBox.SelectedIndex = 0;
        if (BuyerWishCategoryListBox != null) BuyerWishCategoryListBox.SelectedIndex = 0;

        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        RefreshProducts();
        RefreshWishlist();
    }

    // 中正郵件認證
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
            NotificationTextBlock.Text = "⚠️ 請使用中正大學校內 Email 認證！";
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

        LoginOverlayPage.IsVisible = false;
        RefreshProducts();
    }

    private bool IsSchoolEmailValid(string email)
    {
        return email.EndsWith("@ccu.edu.tw", StringComparison.OrdinalIgnoreCase) ||
               email.EndsWith("@alum.ccu.edu.tw", StringComparison.OrdinalIgnoreCase);
    }

    // 發布賣家二手物資
    private void AddProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || NameTextBox == null || PriceTextBox == null || SellerNameTextBox == null) return;

        if (string.IsNullOrWhiteSpace(_currentUserEmail))
        {
            NotificationTextBlock.Text = "⚠️ 請先完成校內帳號認證，才能發布貼文";
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

    // 發布買家期望需求
    private void AddBuyerWish_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || BuyerWishNameTextBox == null || BuyerWishPriceTextBox == null || BuyerWishSellerNameTextBox == null) return;

        if (string.IsNullOrWhiteSpace(_currentUserEmail))
        {
            NotificationTextBlock.Text = "⚠️ 請先完成校內帳號認證，才能發布求購需求";
            return;
        }

        var wishName = BuyerWishNameTextBox.Text?.Trim() ?? "";
        var priceText = BuyerWishPriceTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(wishName) || !decimal.TryParse(priceText, out decimal price))
        {
            NotificationTextBlock.Text = "⚠️ 請填寫正確的求購名稱與期望價格";
            return;
        }

        var product = new Product
        {
            Name = wishName,
            Price = price,
            Description = BuyerWishDescriptionTextBox?.Text?.Trim() ?? "",
            Department = BuyerWishDepartmentTextBox?.Text?.Trim() ?? "未填寫",
            CourseName = BuyerWishCourseNameTextBox?.Text?.Trim() ?? "無",
            Category = GetListBoxText(BuyerWishCategoryListBox, "其他"),
            CampusSection = "買家徵物貼文",
            SellerName = BuyerWishSellerNameTextBox.Text?.Trim() ?? "",
            ContactInfo = BuyerWishContactInfoTextBox?.Text?.Trim() ?? "",
            Location = BuyerWishLocationTextBox?.Text?.Trim() ?? "校內面交",
            VerifiedSchoolEmail = _currentUserEmail,
            CreatedAt = DateTime.Now,
            IsSold = false
        };

        using var db = new AppDbContext();
        db.Products.Add(product);
        db.SaveChanges();

        if (BuyerWishNameTextBox != null) BuyerWishNameTextBox.Text = "";
        if (BuyerWishPriceTextBox != null) BuyerWishPriceTextBox.Text = "";
        if (BuyerWishDescriptionTextBox != null) BuyerWishDescriptionTextBox.Text = "";

        RefreshProducts();
        NotificationTextBlock.Text = "🎉 求購需求成功發布至【買家徵物需求牆】！";
    }

    // 🌟 核心功能一：點擊小圖標按鈕，即時切換展開或收起留言艙！
    private void ToggleCommentBox_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ProductPostViewModel vm)
        {
            vm.IsCommentVisible = !vm.IsCommentVisible;
        }
    }

    // 🌟 核心功能二：即時留言發布與追擊功能！
    private void SubmitComment_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Parent is Grid grid)
        {
            // 動態向上撈取同層的 TextBox 輸入框
            var textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text)) return;

            // 獲取該卡片對應的資料包
            if (textBox.Tag is ProductPostViewModel vm)
            {
                var userInput = textBox.Text.Trim();
                var timeStamp = DateTime.Now.ToString("HH:mm");

                // 如果留言還是初始空話，先清空它
                if (vm.Comments.Count == 1 && vm.Comments[0].Contains("系統提示"))
                {
                    vm.Comments.Clear();
                }

                // 塞入高質感的即時問答格式
                vm.Comments.Add($"💬 [{timeStamp}] {_currentUserName}：{userInput}");
                textBox.Text = ""; // 留言成功送出後自動清空

                if (NotificationTextBlock != null)
                {
                    NotificationTextBlock.Text = $"💬 成功在貼文 [{vm.Name}] 內留下一則即時交易對話！";
                }
            }
        }
    }

    private void MarkAsSoldButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null) return;
        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "⚠️ 請先選取歷史清單中欲標記結案的貼文";
            return;
        }

        using var db = new AppDbContext();
        var product = db.Products.FirstOrDefault(p => p.Id == selectedProduct.Id);

        if (product != null)
        {
            product.IsSold = true;
            db.SaveChanges();
            RefreshProducts();
            NotificationTextBlock.Text = $"已將貼文標記為已結案。🤝 請填寫滿意度交易評價！";
        }
    }

    private void SearchButton_Click(object? sender, RoutedEventArgs e)
    {
        RefreshProducts();
    }

    private void RefreshProducts()
    {
        if (ProductList == null || GraduationProductList == null || BuyerWishProductList == null || SellerManagementPlainListBox == null) return;

        using var db = new AppDbContext();
        var query = db.Products.Where(p => p.CreatedAt >= DateTime.Now.AddDays(-30)).AsQueryable();

        var searchKey = SearchTextBox?.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(searchKey))
        {
            query = query.Where(p => p.Name.Contains(searchKey) || (p.Description != null && p.Description.Contains(searchKey)));
        }

        var categoryFilter = GetListBoxText(FilterCategoryListBox, "全部");
        if (categoryFilter != "全部")
        {
            query = query.Where(p => p.Category == categoryFilter);
        }

        var campusSectionFilter = GetListBoxText(CampusSectionFilterListBox, "全部");
        if (campusSectionFilter != "全部")
        {
            query = query.Where(p => p.CampusSection == campusSectionFilter);
        }

        var statusFilter = GetListBoxText(StatusFilterListBox, "全部");
        if (statusFilter == "販售中")
        {
            query = query.Where(p => !p.IsSold);
        }
        else if (statusFilter == "已售出")
        {
            query = query.Where(p => p.IsSold);
        }

        var deptKey = DepartmentSearchTextBox?.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(deptKey))
        {
            query = query.Where(p => p.Department != null && p.Department.Contains(deptKey));
        }

        var courseKey = CourseSearchTextBox?.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(courseKey))
        {
            query = query.Where(p => p.CourseName != null && p.CourseName.Contains(courseKey));
        }

        if (MinPriceTextBox != null && decimal.TryParse(MinPriceTextBox.Text, out decimal minPrice))
        {
            query = query.Where(p => p.Price >= minPrice);
        }
        if (MaxPriceTextBox != null && decimal.TryParse(MaxPriceTextBox.Text, out decimal maxPrice))
        {
            query = query.Where(p => p.Price <= maxPrice);
        }

        var filteredProducts = query.OrderByDescending(p => p.CreatedAt).ToList();
        _currentProducts = filteredProducts;

        var postViewModels = filteredProducts.Select(p => {
            var daysLeft = 30 - (DateTime.Now.Date - p.CreatedAt.Date).Days;
            return new ProductPostViewModel
            {
                SourceProduct = p,
                DaysLeftText = $"⏳ 剩餘 {Math.Max(0, daysLeft)} 天",
                StatusText = p.IsSold ? "已結案 🤝" : "活躍中 🔥",
                StatusColor = p.IsSold ? "#718096" : "#27AE60"
            };
        }).ToList();

        ProductList.ItemsSource = postViewModels.Where(v => v.CampusSection != "買家徵物貼文").ToList();
        GraduationProductList.ItemsSource = postViewModels.Where(v => v.CampusSection == "畢業傳承商品").ToList();
        BuyerWishProductList.ItemsSource = postViewModels.Where(v => v.CampusSection == "買家徵物貼文").ToList();

        var plainDisplayList = postViewModels.Select(v => $"{v.Name} | ${v.Price} | {v.CampusSection} | {v.StatusText}").ToList();
        SellerManagementPlainListBox.ItemsSource = plainDisplayList;
    }

    private void ViewSellerReviews_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string sellerEmail)
        {
            var overlay = this.FindControl<Grid>("SellerReviewOverlayPage");
            var titleText = this.FindControl<TextBlock>("SellerReviewTitleTextBlock");
            var historyListBox = this.FindControl<ListBox>("SellerReviewHistoryListBox");

            if (overlay == null || historyListBox == null) return;

            using var db = new AppDbContext();
            var historicalSoldProducts = db.Products
                .Where(p => p.VerifiedSchoolEmail == sellerEmail && p.IsSold && !string.IsNullOrEmpty(p.ReviewComment))
                .ToList();

            if (titleText != null)
            {
                titleText.Text = $"👤 用戶 [{btn.Content}] 的歷史信用評價";
            }

            if (historicalSoldProducts.Count == 0)
            {
                historyListBox.ItemsSource = new List<SellerReviewDisplayItem>
                {
                    new SellerReviewDisplayItem { Name = "系統提示", ReviewRatingText = "⭐ 0.0", ReviewComment = "該用戶目前在校內市集尚無交易回饋紀錄。🤝" }
                };
            }
            else
            {
                var reviewItems = historicalSoldProducts.Select(p => new SellerReviewDisplayItem
                {
                    Name = $"📦 成交紀錄：{p.Name}",
                    ReviewRatingText = $"⭐ {(p.ReviewRating > 0 ? p.ReviewRating.ToString() : "5")}.0",
                    ReviewComment = p.ReviewComment ?? "雙方面交過程非常順暢，預設滿分！"
                }).ToList();

                historyListBox.ItemsSource = reviewItems;
            }

            overlay.IsVisible = true;
        }
    }

    private void CloseSellerReviewOverlay_Click(object? sender, RoutedEventArgs e)
    {
        var overlay = this.FindControl<Grid>("SellerReviewOverlayPage");
        if (overlay != null) overlay.IsVisible = false;
    }

    private void CheckWishlistNotification(Product product)
    {
        using var db = new AppDbContext();
        var wishlistItems = db.WishlistItems.ToList();
        var matched = wishlistItems.Where(w => product.Name.Contains(w.Keyword) || (product.Description != null && product.Description.Contains(w.Keyword))).Select(w => w.Keyword).ToList();

        if (matched.Count > 0)
        {
            _bellNotifications.Insert(0, $"[{DateTime.Now:HH:mm}] 🎉 許願匹配：出現了符合您追蹤「{string.Join("、", matched)}」的物資需求：{product.Name}！");
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
        if (NotificationTextBlock != null) NotificationTextBlock.Text = $"🔔 右上角智慧小鈴鐺響起！有全新動態更新了！";
        var dot = this.FindControl<Control>("NewNotificationDot");
        if (dot != null) dot.IsVisible = true;
    }

    private void NotificationBellButton_Click(object? sender, RoutedEventArgs e)
    {
        if (BellNotificationPanel == null || CloseBellPanelButton == null) return;
        BellNotificationPanel.IsVisible = true;
        CloseBellPanelButton.IsVisible = true;

        var dot = this.FindControl<Control>("NewNotificationDot");
        if (dot != null) dot.IsVisible = false;
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
    private void OnGoToBuyerWishPage_Click(object? sender, RoutedEventArgs e) { if (MainWebsiteTabControl != null) MainWebsiteTabControl.SelectedIndex = 2; }
    private void OnGoToSellerPage_Click(object? sender, RoutedEventArgs e) { if (MainWebsiteTabControl != null) MainWebsiteTabControl.SelectedIndex = 3; }

    private void ClearSearchButton_Click(object? sender, RoutedEventArgs e)
    {
        if (SearchTextBox != null) SearchTextBox.Text = "";
        if (MinPriceTextBox != null) MinPriceTextBox.Text = "";
        if (MaxPriceTextBox != null) MaxPriceTextBox.Text = "";
        if (DepartmentSearchTextBox != null) DepartmentSearchTextBox.Text = "";
        if (CourseSearchTextBox != null) CourseSearchTextBox.Text = "";
        if (FilterCategoryListBox != null) FilterCategoryListBox.SelectedIndex = 0;
        if (CampusSectionFilterListBox != null) CampusSectionFilterListBox.SelectedIndex = 0;
        if (StatusFilterListBox != null) StatusFilterListBox.SelectedIndex = 0;
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
        ReviewCommentTextBox.Text = prod.ReviewComment ?? "物資完成面交，過程順暢愉快！";
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
            if (NotificationTextBlock != null) NotificationTextBlock.Text = "🎉 滿意度評價儲存成功！";
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
        if (box == null) return def;

        if (box is ComboBox cb)
        {
            if (cb.SelectedItem is ListBoxItem lbi) return lbi.Content?.ToString() ?? def;
            if (cb.SelectedItem is string strText) return strText;
            return cb.SelectedItem?.ToString() ?? def;
        }

        if (box is ListBox lb)
        {
            if (lb.SelectedItem is ListBoxItem li) return li.Content?.ToString() ?? def;
            if (lb.SelectedItem is string lbStr) return lbStr;
            return lb.SelectedItem?.ToString() ?? def;
        }

        return def;
    }

    private void RecommendedMeetingLocationListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var loc = GetListBoxText(RecommendedMeetingLocationListBox, "");
        if (!string.IsNullOrWhiteSpace(loc))
        {
            if (LocationTextBox != null) LocationTextBox.Text = loc;
            if (BuyerWishLocationTextBox != null) BuyerWishLocationTextBox.Text = loc;
        }
    }

    private void ClearInputFields()
    {
        if (NameTextBox != null) NameTextBox.Text = "";
        if (PriceTextBox != null) CustomUpdatePriceTextBox();
        if (DescriptionTextBox != null) DescriptionTextBox.Text = "";
    }

    private void CustomUpdatePriceTextBox()
    {
        if (PriceTextBox != null) PriceTextBox.Text = "";
    }
}