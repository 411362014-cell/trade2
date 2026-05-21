using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CCUTrade.Data;
using CCUTrade.Models;

namespace CCUTrade;

public partial class MainWindow : Window
{
    private List<Product> _currentProducts = new();
    private List<WishlistItem> _currentWishlistItems = new();

    private string _currentUserName = "";
    private string _currentUserEmail = "";

    public MainWindow()
    {
        // 1. 載入前台 XAML 介面
        Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);

        // 🌟 終極防禦：手動幫後台「全體控制項變數」與前台 XAML 實體精準綁定相認
        // 這樣能徹底防止任何一個控制項在執行時變成 null 導致閃退！
        CategoryListBox = this.FindControl<ComboBox>("CategoryListBox");
        CampusSectionListBox = this.FindControl<ComboBox>("CampusSectionListBox");
        FilterCategoryListBox = this.FindControl<ComboBox>("FilterCategoryListBox");
        CampusSectionFilterListBox = this.FindControl<ComboBox>("CampusSectionFilterListBox");
        StatusFilterListBox = this.FindControl<ComboBox>("StatusFilterListBox");
        ReviewRatingListBox = this.FindControl<ComboBox>("ReviewRatingListBox");

        ProductList = this.FindControl<ListBox>("ProductList");
        WishlistList = this.FindControl<ListBox>("WishlistList");
        RecommendedMeetingLocationListBox = this.FindControl<ListBox>("RecommendedMeetingLocationListBox");

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

        // 2. 確定都安全相認後，設定下拉選單預設選取項目
        if (CategoryListBox != null) CategoryListBox.SelectedIndex = 0;
        if (CampusSectionListBox != null) CampusSectionListBox.SelectedIndex = 0;
        if (FilterCategoryListBox != null) FilterCategoryListBox.SelectedIndex = 0;
        if (CampusSectionFilterListBox != null) CampusSectionFilterListBox.SelectedIndex = 0;
        if (StatusFilterListBox != null) StatusFilterListBox.SelectedIndex = 0;
        if (ReviewRatingListBox != null) ReviewRatingListBox.SelectedIndex = 0;

        // 3. 初始化並確保 SQLite 資料庫建置完成
        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        // 4. 刷新大廳與願望清單
        RefreshProducts();
        RefreshWishlist();
    }

    private void RegisterOrLoginButton_Click(object? sender, RoutedEventArgs e)
    {
        if (StudentNameTextBox == null || SchoolEmailTextBox == null || NotificationTextBlock == null || CurrentUserTextBlock == null || LoginOverlayPage == null) return;

        var studentName = StudentNameTextBox.Text?.Trim() ?? "";
        var schoolEmail = SchoolEmailTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(studentName))
        {
            NotificationTextBlock.Text = "請輸入學生姓名";
            return;
        }

        if (string.IsNullOrWhiteSpace(schoolEmail))
        {
            NotificationTextBlock.Text = "請輸入學校 Email";
            return;
        }

        if (!IsSchoolEmailValid(schoolEmail))
        {
            NotificationTextBlock.Text = "請使用中正大學校內 Email 註冊，例如：abc123@ccu.edu.tw";
            return;
        }

        using var db = new AppDbContext();

        var existingUser = db.UserAccounts.FirstOrDefault(u => u.SchoolEmail == schoolEmail);

        if (existingUser == null)
        {
            var user = new UserAccount
            {
                StudentName = studentName,
                SchoolEmail = schoolEmail,
                CreatedAt = DateTime.Now
            };

            db.UserAccounts.Add(user);
            db.SaveChanges();
        }
        else
        {
            existingUser.StudentName = studentName;
            db.SaveChanges();
        }

        _currentUserName = studentName;
        _currentUserEmail = schoolEmail;

        CurrentUserTextBlock.Text = $"目前登入帳號：{_currentUserName}（{_currentUserEmail}）";
        NotificationTextBlock.Text = $"已完成校內帳號認證：{_currentUserName}（{_currentUserEmail}）";

        // 認證成功後，前台的 Gmail 登入鎖定畫面立刻自動蒸發隱藏！
        LoginOverlayPage.IsVisible = false;
    }

    private bool IsSchoolEmailValid(string email)
    {
        return email.EndsWith("@ccu.edu.tw", StringComparison.OrdinalIgnoreCase) ||
               email.EndsWith("@alum.ccu.edu.tw", StringComparison.OrdinalIgnoreCase);
    }

    private void AddProduct_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || NameTextBox == null || DescriptionTextBox == null || DepartmentTextBox == null ||
            CourseNameTextBox == null || PriceTextBox == null || SellerNameTextBox == null || ContactInfoTextBox == null || LocationTextBox == null) return;

        if (string.IsNullOrWhiteSpace(_currentUserEmail))
        {
            NotificationTextBlock.Text = "請先完成校內帳號認證，才能新增商品";
            return;
        }

        var name = NameTextBox.Text?.Trim() ?? "";
        var description = DescriptionTextBox.Text?.Trim() ?? "";
        var department = DepartmentTextBox.Text?.Trim() ?? "";
        var courseName = CourseNameTextBox.Text?.Trim() ?? "";
        var priceText = PriceTextBox.Text?.Trim() ?? "";
        var sellerName = SellerNameTextBox.Text?.Trim() ?? "";
        var contactInfo = ContactInfoTextBox.Text?.Trim() ?? "";
        var location = LocationTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            NotificationTextBlock.Text = "請輸入商品名稱";
            return;
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            NotificationTextBlock.Text = "請輸入科系，例如：資管系";
            return;
        }

        if (string.IsNullOrWhiteSpace(courseName))
        {
            NotificationTextBlock.Text = "請輸入課程名稱，例如：計算機概論";
            return;
        }

        if (!decimal.TryParse(priceText, out decimal price))
        {
            NotificationTextBlock.Text = "價格請輸入數字，例如：300";
            return;
        }

        if (string.IsNullOrWhiteSpace(sellerName))
        {
            NotificationTextBlock.Text = "請輸入賣家姓名";
            return;
        }

        if (string.IsNullOrWhiteSpace(contactInfo))
        {
            NotificationTextBlock.Text = "請輸入聯絡方式";
            return;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            NotificationTextBlock.Text = "請輸入交易地點";
            return;
        }

        var category = GetListBoxText(CategoryListBox, "其他");
        var campusSection = GetListBoxText(CampusSectionListBox, "一般商品");

        var product = new Product
        {
            Name = name,
            Description = description,
            Department = department,
            CourseName = courseName,
            Category = category,
            Price = price,
            IsSold = false,
            CreatedAt = DateTime.Now,
            SellerName = sellerName,
            ContactInfo = contactInfo,
            Location = location,
            CampusSection = campusSection,
            VerifiedSchoolEmail = _currentUserEmail
        };

        using var db = new AppDbContext();
        db.Products.Add(product);
        db.SaveChanges();

        ClearInputFields();

        RefreshProducts();
        CheckWishlistNotification(product);
    }

    private void RecommendedMeetingLocationListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LocationTextBox == null || NotificationTextBlock == null) return;
        var location = GetListBoxText(RecommendedMeetingLocationListBox, "");

        if (!string.IsNullOrWhiteSpace(location))
        {
            LocationTextBox.Text = location;
            NotificationTextBlock.Text = $"已選擇推薦面交地點：{location}。建議於公開場所面交。";
        }
    }

    private void AddWishlistButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WishlistKeywordTextBox == null || NotificationTextBlock == null) return;
        var keyword = WishlistKeywordTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(keyword))
        {
            NotificationTextBlock.Text = "請輸入願望清單關鍵字";
            return;
        }

        using var db = new AppDbContext();

        var exists = db.WishlistItems.Any(w => w.Keyword == keyword);

        if (exists)
        {
            NotificationTextBlock.Text = $"「{keyword}」已經在願望清單裡了";
            return;
        }

        var item = new WishlistItem
        {
            Keyword = keyword,
            CreatedAt = DateTime.Now
        };

        db.WishlistItems.Add(item);
        db.SaveChanges();

        WishlistKeywordTextBox.Text = "";
        NotificationTextBlock.Text = $"已加入願望清單：{keyword}";

        RefreshWishlist();
    }

    private void DeleteWishlistButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WishlistList == null || NotificationTextBlock == null) return;
        var selectedIndex = WishlistList.SelectedIndex;

        if (selectedIndex < 0 || selectedIndex >= _currentWishlistItems.Count)
        {
            NotificationTextBlock.Text = "請先選取一個願望清單項目";
            return;
        }

        var selectedWishlist = _currentWishlistItems[selectedIndex];

        using var db = new AppDbContext();

        var item = db.WishlistItems.FirstOrDefault(w => w.Id == selectedWishlist.Id);

        if (item == null)
        {
            NotificationTextBlock.Text = "找不到這個願望清單項目";
            return;
        }

        db.WishlistItems.Remove(item);
        db.SaveChanges();

        NotificationTextBlock.Text = $"已刪除願望清單：{selectedWishlist.Keyword}";

        RefreshWishlist();
    }

    private void SearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void DepartmentSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void CourseSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void PriceRangeTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void FilterCategoryListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void CampusSectionFilterListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void StatusFilterListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshProducts();
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        RefreshProducts();
        RefreshWishlist();
        if (NotificationTextBlock != null) NotificationTextBlock.Text = "已重新整理";
    }

    private void ClearSearchButton_Click(object? sender, RoutedEventArgs e)
    {
        if (SearchTextBox == null || DepartmentSearchTextBox == null || CourseSearchTextBox == null ||
            MinPriceTextBox == null || MaxPriceTextBox == null || FilterCategoryListBox == null ||
            CampusSectionFilterListBox == null || StatusFilterListBox == null || NotificationTextBlock == null) return;

        SearchTextBox.Text = "";
        DepartmentSearchTextBox.Text = "";
        CourseSearchTextBox.Text = "";
        MinPriceTextBox.Text = "";
        MaxPriceTextBox.Text = "";
        FilterCategoryListBox.SelectedIndex = 0;
        CampusSectionFilterListBox.SelectedIndex = 0;
        StatusFilterListBox.SelectedIndex = 0;

        RefreshProducts();
        NotificationTextBlock.Text = "已清除搜尋條件";
    }

    private void LoadSelectedProductButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || NameTextBox == null || DescriptionTextBox == null || DepartmentTextBox == null ||
            CourseNameTextBox == null || PriceTextBox == null || SellerNameTextBox == null || ContactInfoTextBox == null || LocationTextBox == null) return;

        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "請先選取一個商品，再按載入";
            return;
        }

        NameTextBox.Text = selectedProduct.Name;
        DescriptionTextBox.Text = selectedProduct.Description;
        DepartmentTextBox.Text = selectedProduct.Department;
        CourseNameTextBox.Text = selectedProduct.CourseName;
        PriceTextBox.Text = selectedProduct.Price.ToString();
        SellerNameTextBox.Text = selectedProduct.SellerName;
        ContactInfoTextBox.Text = selectedProduct.ContactInfo;
        LocationTextBox.Text = selectedProduct.Location;

        SetListBoxSelectedItem(CategoryListBox, selectedProduct.Category);
        SetListBoxSelectedItem(CampusSectionListBox, selectedProduct.CampusSection);

        NotificationTextBlock.Text = $"已載入商品：{selectedProduct.Name}";
    }

    private void UpdateProductButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || NameTextBox == null || DescriptionTextBox == null || DepartmentTextBox == null ||
            CourseNameTextBox == null || PriceTextBox == null || SellerNameTextBox == null || ContactInfoTextBox == null || LocationTextBox == null) return;

        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "請先選取一個商品，再按儲存修改";
            return;
        }

        var name = NameTextBox.Text?.Trim() ?? "";
        var description = DescriptionTextBox.Text?.Trim() ?? "";
        var department = DepartmentTextBox.Text?.Trim() ?? "";
        var courseName = CourseNameTextBox.Text?.Trim() ?? "";
        var priceText = PriceTextBox.Text?.Trim() ?? "";
        var sellerName = SellerNameTextBox.Text?.Trim() ?? "";
        var contactInfo = ContactInfoTextBox.Text?.Trim() ?? "";
        var location = LocationTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            NotificationTextBlock.Text = "請輸入商品名稱";
            return;
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            NotificationTextBlock.Text = "請輸入科系，例如：資管系";
            return;
        }

        if (string.IsNullOrWhiteSpace(courseName))
        {
            NotificationTextBlock.Text = "請輸入課程名稱，例如：計算機概論";
            return;
        }

        if (!decimal.TryParse(priceText, out decimal price))
        {
            NotificationTextBlock.Text = "價格請輸入數字，例如：300";
            return;
        }

        if (string.IsNullOrWhiteSpace(sellerName))
        {
            NotificationTextBlock.Text = "請輸入賣家姓名";
            return;
        }

        if (string.IsNullOrWhiteSpace(contactInfo))
        {
            NotificationTextBlock.Text = "請輸入聯絡方式";
            return;
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            NotificationTextBlock.Text = "請輸入交易地點";
            return;
        }

        var category = GetListBoxText(CategoryListBox, "其他");
        var campusSection = GetListBoxText(CampusSectionListBox, "一般商品");

        using var db = new AppDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == selectedProduct.Id);

        if (product == null)
        {
            NotificationTextBlock.Text = "找不到這筆商品資料";
            return;
        }

        product.Name = name;
        product.Description = description;
        product.Department = department;
        product.CourseName = courseName;
        product.Category = category;
        product.Price = price;
        product.SellerName = sellerName;
        product.ContactInfo = contactInfo;
        product.Location = location;
        product.CampusSection = campusSection;

        db.SaveChanges();

        ClearInputFields();
        RefreshProducts();

        NotificationTextBlock.Text = $"已儲存修改：{product.Name}";
    }

    private void MarkAsSoldButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null) return;
        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "請先選取一個商品，再按已售出";
            return;
        }

        using var db = new AppDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == selectedProduct.Id);

        if (product == null)
        {
            NotificationTextBlock.Text = "找不到這筆商品資料";
            return;
        }

        product.IsSold = true;
        db.SaveChanges();

        RefreshProducts();

        NotificationTextBlock.Text = $"已標記為已售出：{product.Name}，現在可以進行交易評價。";
    }

    private void LoadReviewButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || ReviewCommentTextBox == null) return;
        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "請先選取一個商品，再載入評價";
            return;
        }

        if (!selectedProduct.IsSold)
        {
            NotificationTextBlock.Text = "此商品尚未標記為已售出，完成交易後才能評價";
            return;
        }

        if (selectedProduct.ReviewRating > 0)
        {
            SetListBoxSelectedItem(ReviewRatingListBox, selectedProduct.ReviewRating.ToString());
            ReviewCommentTextBox.Text = selectedProduct.ReviewComment;
            NotificationTextBlock.Text = $"已載入評價：{selectedProduct.Name}";
        }
        else
        {
            if (ReviewRatingListBox is ComboBox combo) combo.SelectedIndex = 0;
            ReviewCommentTextBox.Text = "";
            NotificationTextBlock.Text = $"此商品尚未評價：{selectedProduct.Name}";
        }
    }

    private void SaveReviewButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null || ReviewCommentTextBox == null) return;
        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "請先選取一個已售出商品，再儲存評價";
            return;
        }

        if (!selectedProduct.IsSold)
        {
            NotificationTextBlock.Text = "此商品尚未標記為已售出，完成交易後才能評價";
            return;
        }

        var ratingText = GetListBoxText(ReviewRatingListBox, "5");

        if (!int.TryParse(ratingText, out int rating))
        {
            NotificationTextBlock.Text = "請選擇星等評分";
            return;
        }

        var comment = ReviewCommentTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(comment))
        {
            NotificationTextBlock.Text = "請輸入交易評語";
            return;
        }

        using var db = new AppDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == selectedProduct.Id);

        if (product == null)
        {
            NotificationTextBlock.Text = "找不到這筆商品資料";
            return;
        }

        product.ReviewRating = rating;
        product.ReviewComment = comment;
        product.ReviewedAt = DateTime.Now;

        db.SaveChanges();

        if (ReviewRatingListBox is ComboBox combo) combo.SelectedIndex = 0;
        ReviewCommentTextBox.Text = "";

        RefreshProducts();

        NotificationTextBlock.Text = $"已完成交易評價：{product.Name}，{rating} 星";
    }

    private void DeleteProductButton_Click(object? sender, RoutedEventArgs e)
    {
        if (NotificationTextBlock == null) return;
        var selectedProduct = GetSelectedProduct();

        if (selectedProduct == null)
        {
            NotificationTextBlock.Text = "請先選取一個商品，再按刪除";
            return;
        }

        using var db = new AppDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == selectedProduct.Id);

        if (product == null)
        {
            NotificationTextBlock.Text = "找不到這筆商品資料";
            return;
        }

        db.Products.Remove(product);
        db.SaveChanges();

        RefreshProducts();

        NotificationTextBlock.Text = $"已刪除商品：{selectedProduct.Name}";
    }

    private Product? GetSelectedProduct()
    {
        if (ProductList == null) return null;
        var selectedIndex = ProductList.SelectedIndex;

        if (selectedIndex < 0 || selectedIndex >= _currentProducts.Count)
        {
            return null;
        }

        return _currentProducts[selectedIndex];
    }

    private string GetListBoxText(Control? listBoxOrComboBox, string defaultValue)
    {
        if (listBoxOrComboBox == null) return defaultValue;
        if (listBoxOrComboBox is ListBox listBox && listBox.SelectedItem is ListBoxItem listItem)
        {
            return listItem.Content?.ToString() ?? defaultValue;
        }
        else if (listBoxOrComboBox is ComboBox comboBox && comboBox.SelectedItem is ListBoxItem comboItem)
        {
            return comboItem.Content?.ToString() ?? defaultValue;
        }

        return defaultValue;
    }

    private void SetListBoxSelectedItem(Control? listBoxOrComboBox, string value)
    {
        if (listBoxOrComboBox == null) return;
        if (listBoxOrComboBox is ListBox listBox)
        {
            for (int i = 0; i < listBox.ItemCount; i++)
            {
                if (listBox.Items[i] is ListBoxItem item && item.Content?.ToString() == value)
                {
                    listBox.SelectedIndex = i;
                    return;
                }
            }
            listBox.SelectedIndex = 0;
        }
        else if (listBoxOrComboBox is ComboBox comboBox)
        {
            for (int i = 0; i < comboBox.ItemCount; i++)
            {
                if (comboBox.Items[i] is ListBoxItem item && item.Content?.ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
            comboBox.SelectedIndex = 0;
        }
    }

    private void ClearInputFields()
    {
        if (NameTextBox != null) NameTextBox.Text = "";
        if (DescriptionTextBox != null) DescriptionTextBox.Text = "";
        if (DepartmentTextBox != null) DepartmentTextBox.Text = "";
        if (CourseNameTextBox != null) CourseNameTextBox.Text = "";
        if (PriceTextBox != null) PriceTextBox.Text = "";
        if (SellerNameTextBox != null) SellerNameTextBox.Text = "";
        if (ContactInfoTextBox != null) ContactInfoTextBox.Text = "";
        if (LocationTextBox != null) LocationTextBox.Text = "";
        if (CategoryListBox is ComboBox c1) c1.SelectedIndex = 0;
        if (CampusSectionListBox is ComboBox c2) c2.SelectedIndex = 0;
        if (RecommendedMeetingLocationListBox != null) RecommendedMeetingLocationListBox.SelectedIndex = -1;
    }

    private void RefreshProducts()
    {
        if (SearchTextBox == null || ProductList == null || FilterCategoryListBox == null ||
            CampusSectionFilterListBox == null || StatusFilterListBox == null || DepartmentSearchTextBox == null ||
            CourseSearchTextBox == null || MinPriceTextBox == null || MaxPriceTextBox == null)
        {
            return;
        }

        using var db = new AppDbContext();

        var keyword = SearchTextBox.Text?.Trim() ?? "";
        var departmentKeyword = DepartmentSearchTextBox.Text?.Trim() ?? "";
        var courseKeyword = CourseSearchTextBox.Text?.Trim() ?? "";
        var selectedCategory = GetListBoxText(FilterCategoryListBox, "全部");
        var selectedCampusSection = GetListBoxText(CampusSectionFilterListBox, "全部");
        var selectedStatus = GetListBoxText(StatusFilterListBox, "販售中");
        var minPriceText = MinPriceTextBox.Text?.Trim() ?? "";
        var maxPriceText = MaxPriceTextBox.Text?.Trim() ?? "";

        var expireDate = DateTime.Now.AddDays(-30);

        var query = db.Products
            .Where(p => p.CreatedAt >= expireDate);

        if (selectedStatus == "販售中")
        {
            query = query.Where(p => !p.IsSold);
        }
        else if (selectedStatus == "已售出")
        {
            query = query.Where(p => p.IsSold);
        }

        if (selectedCategory != "全部")
        {
            query = query.Where(p => p.Category == selectedCategory);
        }

        if (selectedCampusSection != "全部")
        {
            query = query.Where(p => p.CampusSection == selectedCampusSection);
        }

        if (!string.IsNullOrWhiteSpace(departmentKeyword))
        {
            query = query.Where(p => p.Department.Contains(departmentKeyword));
        }

        if (!string.IsNullOrWhiteSpace(courseKeyword))
        {
            query = query.Where(p => p.CourseName.Contains(courseKeyword));
        }

        if (decimal.TryParse(minPriceText, out decimal minPrice))
        {
            query = query.Where(p => p.Price >= minPrice);
        }

        if (decimal.TryParse(maxPriceText, out decimal maxPrice))
        {
            query = query.Where(p => p.Price <= maxPrice);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywords = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in keywords)
            {
                var searchWord = word;

                query = query.Where(p =>
                    p.Name.Contains(searchWord) ||
                    (p.Description != null && p.Description.Contains(searchWord)) ||
                    p.Department.Contains(searchWord) ||
                    p.CourseName.Contains(searchWord) ||
                    p.Category.Contains(searchWord) ||
                    p.SellerName.Contains(searchWord) ||
                    p.ContactInfo.Contains(searchWord) ||
                    p.Location.Contains(searchWord) ||
                    p.CampusSection.Contains(searchWord) ||
                    p.VerifiedSchoolEmail.Contains(searchWord) ||
                    (p.ReviewComment != null && p.ReviewComment.Contains(searchWord)));
            }
        }

        _currentProducts = query
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        var displayList = _currentProducts
            .Select(p =>
            {
                var daysLeft = 30 - (DateTime.Now.Date - p.CreatedAt.Date).Days;

                if (daysLeft < 0)
                {
                    daysLeft = 0;
                }

                var statusText = p.IsSold ? "已售出" : "販售中";
                var reviewText = p.ReviewRating > 0
                    ? $"｜評價：{p.ReviewRating} 星｜評語：{p.ReviewComment}"
                    : "｜尚未評價";

                return $"【{p.CampusSection}】{p.Name}｜科系：{p.Department}｜課程：{p.CourseName}｜{p.Category}｜${p.Price}｜{statusText}｜剩餘 {daysLeft} 天｜認證帳號：{p.VerifiedSchoolEmail}｜賣家：{p.SellerName}｜聯絡：{p.ContactInfo}｜地點：{p.Location}｜{p.Description}{reviewText}";
            })
            .ToList();

        if (displayList.Count == 0)
        {
            displayList.Add("目前沒有符合條件的商品");
        }

        ProductList.ItemsSource = displayList;
    }

    private void RefreshWishlist()
    {
        if (WishlistList == null) return;
        using var db = new AppDbContext();

        _currentWishlistItems = db.WishlistItems
            .OrderByDescending(w => w.CreatedAt)
            .ToList();

        var displayList = _currentWishlistItems
            .Select(w => $"想買：{w.Keyword}")
            .ToList();

        if (displayList.Count == 0)
        {
            displayList.Add("目前願望清單是空的");
        }

        WishlistList.ItemsSource = displayList;
    }

    private void CheckWishlistNotification(Product product)
    {
        if (NotificationTextBlock == null) return;
        using var db = new AppDbContext();

        var wishlistItems = db.WishlistItems.ToList();

        var matchedItems = wishlistItems
            .Where(w =>
                product.Name.Contains(w.Keyword) ||
                (product.Description != null && product.Description.Contains(w.Keyword)) ||
                product.Department.Contains(w.Keyword) ||
                product.CourseName.Contains(w.Keyword) ||
                product.Category.Contains(w.Keyword) ||
                product.CampusSection.Contains(w.Keyword))
            .Select(w => w.Keyword)
            .ToList();

        if (matchedItems.Count > 0)
        {
            NotificationTextBlock.Text = $"🔔 您的願望清單「{string.Join("、", matchedItems)}」有新商品上架：{product.Name}";
        }
        else
        {
            NotificationTextBlock.Text = $"已新增商品：{product.Name}";
        }
    }

    private void OnGoToHomePage_Click(object? sender, RoutedEventArgs e)
    {
        if (MainWebsiteTabControl != null && NotificationTextBlock != null)
        {
            MainWebsiteTabControl.SelectedIndex = 0;
            NotificationTextBlock.Text = "已切換至：校園大廳（智慧搜尋與快速篩選）";
        }
    }

    private void OnGoToSellerPage_Click(object? sender, RoutedEventArgs e)
    {
        if (MainWebsiteTabControl != null && NotificationTextBlock != null)
        {
            MainWebsiteTabControl.SelectedIndex = 1;
            NotificationTextBlock.Text = "已切換至：智慧賣家管理中心（商品上架、有效期限與隱藏評價系統）";
        }
    }
}