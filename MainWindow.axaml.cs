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
        InitializeComponent();

        CategoryListBox.SelectedIndex = 0;
        CampusSectionListBox.SelectedIndex = 0;
        FilterCategoryListBox.SelectedIndex = 0;
        CampusSectionFilterListBox.SelectedIndex = 0;
        StatusFilterListBox.SelectedIndex = 0;
        ReviewRatingListBox.SelectedIndex = 0;

        using var db = new AppDbContext();
        db.Database.EnsureCreated();

        RefreshProducts();
        RefreshWishlist();
    }

    private void RegisterOrLoginButton_Click(object? sender, RoutedEventArgs e)
    {
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
    }

    private bool IsSchoolEmailValid(string email)
    {
        return email.EndsWith("@ccu.edu.tw", StringComparison.OrdinalIgnoreCase) ||
               email.EndsWith("@alum.ccu.edu.tw", StringComparison.OrdinalIgnoreCase);
    }

    private void AddProduct_Click(object? sender, RoutedEventArgs e)
    {
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
        var location = GetListBoxText(RecommendedMeetingLocationListBox, "");

        if (!string.IsNullOrWhiteSpace(location))
        {
            LocationTextBox.Text = location;
            NotificationTextBlock.Text = $"已選擇推薦面交地點：{location}。建議於公開場所面交。";
        }
    }

    private void AddWishlistButton_Click(object? sender, RoutedEventArgs e)
    {
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
        NotificationTextBlock.Text = "已重新整理";
    }

    private void ClearSearchButton_Click(object? sender, RoutedEventArgs e)
    {
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
            ReviewRatingListBox.SelectedIndex = 0;
            ReviewCommentTextBox.Text = "";
            NotificationTextBlock.Text = $"此商品尚未評價：{selectedProduct.Name}";
        }
    }

    private void SaveReviewButton_Click(object? sender, RoutedEventArgs e)
    {
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

        ReviewRatingListBox.SelectedIndex = 0;
        ReviewCommentTextBox.Text = "";

        RefreshProducts();

        NotificationTextBlock.Text = $"已完成交易評價：{product.Name}，{rating} 星";
    }

    private void DeleteProductButton_Click(object? sender, RoutedEventArgs e)
    {
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
        var selectedIndex = ProductList.SelectedIndex;

        if (selectedIndex < 0 || selectedIndex >= _currentProducts.Count)
        {
            return null;
        }

        return _currentProducts[selectedIndex];
    }

    private string GetListBoxText(ListBox listBox, string defaultValue)
    {
        if (listBox.SelectedItem is ListBoxItem item)
        {
            return item.Content?.ToString() ?? defaultValue;
        }

        return defaultValue;
    }

    private void SetListBoxSelectedItem(ListBox listBox, string value)
    {
        for (int i = 0; i < listBox.ItemCount; i++)
        {
            var item = listBox.Items[i] as ListBoxItem;

            if (item?.Content?.ToString() == value)
            {
                listBox.SelectedIndex = i;
                return;
            }
        }

        listBox.SelectedIndex = 0;
    }

    private void ClearInputFields()
    {
        NameTextBox.Text = "";
        DescriptionTextBox.Text = "";
        DepartmentTextBox.Text = "";
        CourseNameTextBox.Text = "";
        PriceTextBox.Text = "";
        SellerNameTextBox.Text = "";
        ContactInfoTextBox.Text = "";
        LocationTextBox.Text = "";
        CategoryListBox.SelectedIndex = 0;
        CampusSectionListBox.SelectedIndex = 0;
        RecommendedMeetingLocationListBox.SelectedIndex = -1;
    }

    private void RefreshProducts()
    {
        if (SearchTextBox == null ||
            ProductList == null ||
            FilterCategoryListBox == null ||
            CampusSectionFilterListBox == null ||
            StatusFilterListBox == null ||
            DepartmentSearchTextBox == null ||
            CourseSearchTextBox == null ||
            MinPriceTextBox == null ||
            MaxPriceTextBox == null)
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
                    p.Description.Contains(searchWord) ||
                    p.Department.Contains(searchWord) ||
                    p.CourseName.Contains(searchWord) ||
                    p.Category.Contains(searchWord) ||
                    p.SellerName.Contains(searchWord) ||
                    p.ContactInfo.Contains(searchWord) ||
                    p.Location.Contains(searchWord) ||
                    p.CampusSection.Contains(searchWord) ||
                    p.VerifiedSchoolEmail.Contains(searchWord) ||
                    p.ReviewComment.Contains(searchWord));
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
        using var db = new AppDbContext();

        var wishlistItems = db.WishlistItems.ToList();

        var matchedItems = wishlistItems
            .Where(w =>
                product.Name.Contains(w.Keyword) ||
                product.Description.Contains(w.Keyword) ||
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
}