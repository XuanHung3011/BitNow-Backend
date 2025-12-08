# Unit Test Case Document - Method List

## Document Information

**Project Name:** BitNow-Backend  
**Document Type:** Unit Test Case Specification  
**Document Title:** Unit Test Case Document - Method List  
**Version:** 1.0  
**Date:** 2025-12-06

---

## Test Environment Setup Description

### 1. Development Environment

**1.1. Operating System**
- Windows 10/11 (64-bit) hoặc tương đương
- Linux (Ubuntu 20.04+) hoặc macOS (10.15+)

**1.2. Development Tools**
- Visual Studio 2022 hoặc Visual Studio Code
- .NET SDK 8.0 hoặc cao hơn
- Git for version control

**1.3. IDE Extensions**
- C# extension for Visual Studio Code (nếu sử dụng VS Code)
- .NET Test Explorer extension

### 2. Runtime Environment

**2.1. .NET Runtime**
- .NET 8.0 Runtime
- .NET 8.0 SDK

**2.2. Test Framework**
- xUnit 2.9.2
- Microsoft.NET.Test.Sdk 17.12.0
- xunit.runner.visualstudio 2.8.2

**2.3. Testing Libraries**
- Moq 4.20.72 (Mocking framework)
- Moq.EntityFrameworkCore 8.0.1.1 (EF Core mocking support)
- FluentAssertions 8.8.0 (Assertion library)
- Microsoft.AspNetCore.Mvc.Testing 8.0.0 (ASP.NET Core testing utilities)
- coverlet.collector 6.0.2 (Code coverage)

### 3. Database

**3.1. Database Server**
- SQL Server 2019+ hoặc SQL Server Express
- Hoặc sử dụng In-Memory Database cho unit tests (không cần database thực tế)

**3.2. Database Context**
- Entity Framework Core 8.0
- BidNowDbContext (mocked trong unit tests)
- Không cần kết nối database thực tế vì sử dụng Moq để mock DbContext

### 4. Dependencies

**4.1. Project References**
- BitNow-Backend.API (main project)
- BitNow-Backend.BLL (business logic layer)
- BitNow-Backend.DAL (data access layer)

**4.2. External Services (Mocked)**
- PayOS Service (payment gateway) - mocked
- Email Service - mocked
- SignalR Hub Context - mocked
- File Upload Service - mocked
- Notification Service - mocked

### 5. Configuration

**5.1. Application Settings**
- appsettings.json (không bắt buộc cho unit tests)
- Tất cả dependencies được mock, không cần configuration thực tế

**5.2. Test Configuration**
- Không cần file cấu hình riêng
- Tất cả test data được tạo trong test methods

### 6. Build Tools

**6.1. Command Line Tools**
- dotnet CLI (included in .NET SDK)
- PowerShell hoặc Bash (cho scripts)

**6.2. Build Commands**
```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

### 7. Test Execution

**7.1. Test Runner**
- xUnit test runner (built-in)
- Visual Studio Test Explorer
- Command line (dotnet test)

**7.2. Test Isolation**
- Mỗi test case độc lập
- Không chia sẻ state giữa các tests
- Sử dụng mocks để isolate dependencies

### 8. Code Coverage

**8.1. Coverage Tools**
- coverlet.collector (code coverage collector)
- ReportGenerator (optional, for HTML reports)

**8.2. Coverage Reports**
- LCOV format (default)
- Có thể export sang HTML, XML, JSON

---

## Method List

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| 1 | AuthController | Register | AuthController_Register | Đăng ký user mới vào hệ thống với email, password và full name | Email chưa tồn tại trong hệ thống, AuthService hoạt động bình thường |
| 2 | AuthController | Login | AuthController_Login | Đăng nhập user với email và password | User đã tồn tại trong hệ thống, email đã được verify, AuthService hoạt động bình thường |
| 3 | AuthController | Verify | AuthController_Verify | Xác minh email user với token | Token hợp lệ và chưa hết hạn, AuthService hoạt động bình thường |
| 4 | AuthController | ForgotPassword | AuthController_ForgotPassword | Yêu cầu reset password, gửi link reset đến email | Email đã được đăng ký trong hệ thống, AuthService hoạt động bình thường |
| 5 | AuthController | ResetPassword | AuthController_ResetPassword | Đặt lại mật khẩu mới với token | Token hợp lệ và chưa hết hạn, AuthService hoạt động bình thường |
| 6 | AuthController | Resend | AuthController_Resend | Gửi lại email xác minh cho user | UserId và Email hợp lệ, AuthService hoạt động bình thường |
| 7 | AuctionsController | Create | AuctionsController_Create | Tạo auction mới với item, seller và starting bid | Item đã được approve, Seller hợp lệ, AuctionService và BidService hoạt động bình thường |
| 8 | AuctionsController | Get | AuctionsController_Get | Lấy thông tin chi tiết auction theo ID | AuctionId hợp lệ, Auction tồn tại trong hệ thống, AuctionService hoạt động bình thường |
| 9 | AuctionsController | PlaceBid | AuctionsController_PlaceBid | Đặt bid cho auction | Auction đang active, Buyer hợp lệ, Bid amount lớn hơn current bid, BidService hoạt động bình thường |
| 10 | AuctionsController | BuyNow | AuctionsController_BuyNow | Mua ngay auction với buy now price | Auction có buy now price, Auction đang active, Buyer hợp lệ, AuctionService hoạt động bình thường |
| 11 | AuctionsController | GetRecentBids | AuctionsController_GetRecentBids | Lấy danh sách bids gần đây của auction | AuctionId hợp lệ, BidService hoạt động bình thường |
| 12 | AuctionsController | GetHighestBid | AuctionsController_GetHighestBid | Lấy bid cao nhất của auction | AuctionId hợp lệ, BidService hoạt động bình thường |
| 13 | AuctionsController | GetActiveBidsByBuyer | AuctionsController_GetActiveBidsByBuyer | Lấy danh sách active bids của buyer | BuyerId hợp lệ, AuctionService hoạt động bình thường |
| 14 | AuctionsController | GetWonAuctionsByBuyer | AuctionsController_GetWonAuctionsByBuyer | Lấy danh sách auctions mà buyer đã thắng | BuyerId hợp lệ, AuctionService hoạt động bình thường |
| 15 | AuctionsController | GetBiddingHistory | AuctionsController_GetBiddingHistory | Lấy lịch sử bidding của buyer | BuyerId hợp lệ, BidService hoạt động bình thường |
| 16 | AuctionsController | GetAuctionsBySeller | AuctionsController_GetAuctionsBySeller | Lấy danh sách auctions của seller | SellerId hợp lệ, AuctionService hoạt động bình thường |
| 17 | AuctionsController | GetAllAuctions | AuctionsController_GetAllAuctions | Lấy danh sách tất cả auctions với filter và pagination | AuctionService hoạt động bình thường |
| 18 | AdminAuctionsController | GetAuctions | AdminAuctionsController_GetAuctions | Lấy danh sách auctions cho admin với filter và pagination | AdminService hoạt động bình thường |
| 19 | AdminAuctionsController | GetAuctionDetail | AdminAuctionsController_GetAuctionDetail | Lấy thông tin chi tiết auction cho admin | AuctionId hợp lệ, Auction tồn tại, AdminService hoạt động bình thường |
| 20 | AdminAuctionsController | UpdateStatus | AdminAuctionsController_UpdateStatus | Cập nhật status của auction (completed, cancelled, paused) | AuctionId hợp lệ, Status hợp lệ, AdminService hoạt động bình thường |
| 21 | AdminAuctionsController | ResumeAuction | AdminAuctionsController_ResumeAuction | Tiếp tục auction đã bị pause | AuctionId hợp lệ, Auction đang ở status paused, EndTime chưa qua, AdminService hoạt động bình thường |
| 22 | AdminStatsController | GetAdminStats | AdminStatsController_GetAdminStats | Lấy thống kê tổng quan cho admin dashboard | AdminService hoạt động bình thường |
| 23 | AdminStatsController | GetAdminStatsDetail | AdminStatsController_GetAdminStatsDetail | Lấy thống kê chi tiết theo type cho admin | Type hợp lệ (revenue, users, auctions, etc.), AdminService hoạt động bình thường |
| 24 | AuctionMessagesController | GetMessages | AuctionMessagesController_GetMessages | Lấy danh sách messages của auction chat | AuctionId hợp lệ, MessageService hoạt động bình thường |
| 25 | AuctionMessagesController | CreateMessage | AuctionMessagesController_CreateMessage | Tạo message mới trong auction chat | AuctionId hợp lệ, UserId hợp lệ, Message content hợp lệ, MessageService và SignalR Hub hoạt động bình thường |
| 26 | AutoBidsController | CreateOrUpdate | AutoBidsController_CreateOrUpdate | Tạo hoặc cập nhật auto bid cho auction | Auction tồn tại và đang active, User hợp lệ, MaxAmount hợp lệ, AutoBidService hoạt động bình thường |
| 27 | AutoBidsController | Get | AutoBidsController_Get | Lấy thông tin auto bid của user cho auction | AuctionId và UserId hợp lệ, AutoBidService hoạt động bình thường |
| 28 | AutoBidsController | Deactivate | AutoBidsController_Deactivate | Vô hiệu hóa auto bid | AutoBidId hợp lệ, AutoBid tồn tại, AutoBidService hoạt động bình thường |
| 29 | AutoBidsController | GetBidIncrement | AutoBidsController_GetBidIncrement | Lấy giá trị bid increment cho auction | AuctionId hợp lệ, AutoBidService hoạt động bình thường |
| 30 | CategoriesController | GetAllCategories | CategoriesController_GetAllCategories | Lấy danh sách tất cả categories | CategoryService hoạt động bình thường |
| 31 | CategoriesController | GetCategory | CategoriesController_GetCategory | Lấy thông tin category theo ID | CategoryId hợp lệ, Category tồn tại, CategoryService hoạt động bình thường |
| 32 | CategoriesController | GetCategoryBySlug | CategoriesController_GetCategoryBySlug | Lấy thông tin category theo slug | Slug hợp lệ, Category tồn tại, CategoryService hoạt động bình thường |
| 33 | CategoriesController | CreateCategory | CategoriesController_CreateCategory | Tạo category mới | Category data hợp lệ, Slug chưa tồn tại, CategoryService hoạt động bình thường |
| 34 | CategoriesController | UpdateCategory | CategoriesController_UpdateCategory | Cập nhật thông tin category | CategoryId hợp lệ, Category data hợp lệ, CategoryService hoạt động bình thường |
| 35 | CategoriesController | DeleteCategory | CategoriesController_DeleteCategory | Xóa category | CategoryId hợp lệ, Category không đang được sử dụng, CategoryService hoạt động bình thường |
| 36 | CategoriesController | GetCategoriesPaged | CategoriesController_GetCategoriesPaged | Lấy danh sách categories với pagination | CategoryService hoạt động bình thường |
| 37 | CategoriesController | CheckSlugExists | CategoriesController_CheckSlugExists | Kiểm tra slug đã tồn tại chưa | Slug hợp lệ, CategoryService hoạt động bình thường |
| 38 | CategoriesController | IsCategoryInUse | CategoriesController_IsCategoryInUse | Kiểm tra category có đang được sử dụng không | CategoryId hợp lệ, CategoryService hoạt động bình thường |
| 39 | FavoriteSellersController | GetMyFavorites | FavoriteSellersController_GetMyFavorites | Lấy danh sách favorite sellers của user | UserId hợp lệ (từ header X-User-Id), FavoriteSellerService hoạt động bình thường |
| 40 | FavoriteSellersController | CheckIsFavorite | FavoriteSellersController_CheckIsFavorite | Kiểm tra seller có trong favorite list không | UserId và SellerId hợp lệ, FavoriteSellerService hoạt động bình thường |
| 41 | FavoriteSellersController | AddFavorite | FavoriteSellersController_AddFavorite | Thêm seller vào favorite list | UserId và SellerId hợp lệ, Seller chưa có trong favorite list, FavoriteSellerService hoạt động bình thường |
| 42 | FavoriteSellersController | RemoveFavorite | FavoriteSellersController_RemoveFavorite | Xóa seller khỏi favorite list | UserId và SellerId hợp lệ, Seller đã có trong favorite list, FavoriteSellerService hoạt động bình thường |
| 43 | HomeController | GetAllItems | HomeController_GetAllItems | Lấy danh sách tất cả approved items | ItemService hoạt động bình thường |
| 44 | HomeController | GetAllItemsPaged | HomeController_GetAllItemsPaged | Lấy danh sách approved items với pagination | ItemService hoạt động bình thường |
| 45 | HomeController | SearchItems | HomeController_SearchItems | Tìm kiếm approved items theo search term | SearchTerm hợp lệ (không rỗng), ItemService hoạt động bình thường |
| 46 | HomeController | SearchItemsPaged | HomeController_SearchItemsPaged | Tìm kiếm approved items với pagination | SearchTerm hợp lệ (không rỗng), ItemService hoạt động bình thường |
| 47 | HomeController | FilterItems | HomeController_FilterItems | Lọc approved items theo filter criteria | FilterDto hợp lệ, ItemService hoạt động bình thường |
| 48 | HomeController | GetCategories | HomeController_GetCategories | Lấy danh sách categories | ItemService hoạt động bình thường |
| 49 | HomeController | GetHot | HomeController_GetHot | Lấy danh sách hot auctions | ItemService hoạt động bình thường |
| 50 | ItemsController | CreateItem | ItemsController_CreateItem | Tạo item mới với multipart/form-data (bao gồm images) | SellerId hợp lệ, CategoryId hợp lệ, Title hợp lệ, Images hợp lệ, ItemService, FileUploadService, NotificationService hoạt động bình thường |
| 51 | ItemsController | CreateDraftItem | ItemsController_CreateDraftItem | Tạo draft item (chưa submit) | SellerId hợp lệ, Item data hợp lệ, ItemService hoạt động bình thường |
| 52 | ItemsController | UpdateDraftItem | ItemsController_UpdateDraftItem | Cập nhật draft item | ItemId hợp lệ, Item ở trạng thái draft, Item data hợp lệ, ItemService hoạt động bình thường |
| 53 | ItemsController | GetAllItems | ItemsController_GetAllItems | Lấy danh sách items với filter và pagination (admin) | ItemService hoạt động bình thường |
| 54 | ItemsController | ApproveItem | ItemsController_ApproveItem | Phê duyệt item (admin) | ItemId hợp lệ, Item ở trạng thái pending, ItemService, NotificationService, SignalR Hub hoạt động bình thường |
| 55 | ItemsController | RejectItem | ItemsController_RejectItem | Từ chối item với lý do (admin) | ItemId hợp lệ, Item ở trạng thái pending, RejectReason hợp lệ, ItemService, NotificationService hoạt động bình thường |
| 56 | ItemsController | GetItemById | ItemsController_GetItemById | Lấy thông tin chi tiết item theo ID | ItemId hợp lệ, Item tồn tại, ItemService hoạt động bình thường |
| 57 | ItemsController | DeleteItem | ItemsController_DeleteItem | Xóa item (chỉ draft items) | ItemId hợp lệ, Item ở trạng thái draft, ItemService hoạt động bình thường |
| 58 | MessagesController | SendMessage | MessagesController_SendMessage | Gửi message giữa users | SenderId và ReceiverId hợp lệ, Message content hợp lệ, MessageService hoạt động bình thường |
| 59 | MessagesController | GetConversations | MessagesController_GetConversations | Lấy danh sách conversations của user | UserId hợp lệ, MessageService hoạt động bình thường |
| 60 | MessagesController | GetConversation | MessagesController_GetConversation | Lấy messages của một conversation | UserId và OtherUserId hợp lệ, MessageService hoạt động bình thường |
| 61 | MessagesController | MarkAsRead | MessagesController_MarkAsRead | Đánh dấu message đã đọc | MessageId hợp lệ, Message tồn tại, MessageService hoạt động bình thường |
| 62 | MessagesController | GetUnreadMessages | MessagesController_GetUnreadMessages | Lấy danh sách unread messages của user | UserId hợp lệ, MessageService hoạt động bình thường |
| 63 | MessagesController | GetAllMessages | MessagesController_GetAllMessages | Lấy tất cả messages của user | UserId hợp lệ, MessageService hoạt động bình thường |
| 64 | NotificationsController | GetNotifications | NotificationsController_GetNotifications | Lấy danh sách notifications của user | UserId hợp lệ, NotificationService hoạt động bình thường |
| 65 | NotificationsController | GetUnreadNotifications | NotificationsController_GetUnreadNotifications | Lấy danh sách unread notifications của user | UserId hợp lệ, NotificationService hoạt động bình thường |
| 66 | NotificationsController | GetUnreadCount | NotificationsController_GetUnreadCount | Lấy số lượng unread notifications | UserId hợp lệ, NotificationService hoạt động bình thường |
| 67 | NotificationsController | CreateNotification | NotificationsController_CreateNotification | Tạo notification mới | NotificationDto hợp lệ, NotificationService hoạt động bình thường |
| 68 | NotificationsController | MarkAsRead | NotificationsController_MarkAsRead | Đánh dấu notification đã đọc | NotificationId hợp lệ, Notification tồn tại, NotificationService hoạt động bình thường |
| 69 | NotificationsController | MarkAllAsRead | NotificationsController_MarkAllAsRead | Đánh dấu tất cả notifications đã đọc | UserId hợp lệ, User tồn tại, NotificationService hoạt động bình thường |
| 70 | NotificationsController | DeleteNotification | NotificationsController_DeleteNotification | Xóa notification | NotificationId hợp lệ, Notification tồn tại, NotificationService hoạt động bình thường |
| 71 | PaymentController | CreatePaymentLink | PaymentController_CreatePaymentLink | Tạo payment link cho order qua PayOS | OrderId hợp lệ, Order tồn tại, Order ở trạng thái awaiting_payment, PayOSService và OrderService hoạt động bình thường |
| 72 | PaymentController | HandleWebhook | PaymentController_HandleWebhook | Xử lý webhook từ PayOS khi payment thành công/thất bại | Webhook data hợp lệ, PayOSService và OrderService hoạt động bình thường |
| 73 | PaymentController | GetOrderByAuctionId | PaymentController_GetOrderByAuctionId | Lấy order theo auction ID | AuctionId hợp lệ, OrderService hoạt động bình thường |
| 74 | PaymentController | GetBuyerOrders | PaymentController_GetBuyerOrders | Lấy danh sách orders của buyer | BuyerId hợp lệ, OrderService hoạt động bình thường |
| 75 | PaymentController | GetSellerOrders | PaymentController_GetSellerOrders | Lấy danh sách orders của seller | SellerId hợp lệ, OrderService hoạt động bình thường |
| 76 | PaymentController | UpdateShippingInfo | PaymentController_UpdateShippingInfo | Cập nhật thông tin shipping cho order | OrderId hợp lệ, Order ở trạng thái paid, ShippingInfo hợp lệ, OrderService hoạt động bình thường |
| 77 | PaymentController | ConfirmOrderReceived | PaymentController_ConfirmOrderReceived | Xác nhận đã nhận hàng (buyer) | OrderId hợp lệ, Order ở trạng thái shipped, OrderService hoạt động bình thường |
| 78 | PaymentController | ReportOrderIssue | PaymentController_ReportOrderIssue | Báo cáo vấn đề với order | OrderId hợp lệ, Order tồn tại, Issue description hợp lệ, OrderService hoạt động bình thường |
| 79 | PlatformAnalyticsController | GetPlatformAnalytics | PlatformAnalyticsController_GetPlatformAnalytics | Lấy thống kê tổng quan của platform | PlatformAnalyticsService hoạt động bình thường |
| 80 | PlatformAnalyticsController | GetAnalyticsDetail | PlatformAnalyticsController_GetAnalyticsDetail | Lấy thống kê chi tiết theo type | Type hợp lệ (users, auctions, revenue, etc.), PlatformAnalyticsService hoạt động bình thường |
| 81 | RatingsController | Create | RatingsController_Create | Tạo rating cho auction | AuctionId hợp lệ, UserId hợp lệ, Rating data hợp lệ, RatingService hoạt động bình thường |
| 82 | RatingsController | GetForUser | RatingsController_GetForUser | Lấy ratings của user | UserId hợp lệ, RatingService hoạt động bình thường |
| 83 | RatingsController | GetForAuction | RatingsController_GetForAuction | Lấy ratings của auction | AuctionId hợp lệ, RatingService hoạt động bình thường |
| 84 | RecommendationsController | GetPersonalized | RecommendationsController_GetPersonalized | Lấy danh sách items được recommend cho user | UserId hợp lệ, RecommendationService hoạt động bình thường |
| 85 | SellerStatsController | GetSellerStats | SellerStatsController_GetSellerStats | Lấy thống kê tổng quan của seller | SellerId hợp lệ, SellerStatsService hoạt động bình thường |
| 86 | SellerStatsController | GetSellerStatsDetail | SellerStatsController_GetSellerStatsDetail | Lấy thống kê chi tiết của seller theo type | SellerId hợp lệ, Type hợp lệ, SellerStatsService hoạt động bình thường |
| 87 | UsersController | GetUsers | UsersController_GetUsers | Lấy danh sách users với filter và pagination | UserService hoạt động bình thường |
| 88 | UsersController | GetUser | UsersController_GetUser | Lấy thông tin user theo ID | UserId hợp lệ, User tồn tại, UserService hoạt động bình thường |
| 89 | UsersController | GetUserByEmail | UsersController_GetUserByEmail | Lấy thông tin user theo email | Email hợp lệ, User tồn tại, UserService hoạt động bình thường |
| 90 | UsersController | CreateUser | UsersController_CreateUser | Tạo user mới (admin) | User data hợp lệ, Email chưa tồn tại, UserService hoạt động bình thường |
| 91 | UsersController | UpdateUser | UsersController_UpdateUser | Cập nhật thông tin user | UserId hợp lệ, User data hợp lệ, UserService hoạt động bình thường |
| 92 | UsersController | ChangePassword | UsersController_ChangePassword | Đổi mật khẩu user | UserId hợp lệ, CurrentPassword đúng, NewPassword hợp lệ, UserService hoạt động bình thường |
| 93 | UsersController | ActivateUser | UsersController_ActivateUser | Kích hoạt user | UserId hợp lệ, User tồn tại, UserService hoạt động bình thường |
| 94 | UsersController | DeactivateUser | UsersController_DeactivateUser | Vô hiệu hóa user | UserId hợp lệ, User tồn tại, UserService hoạt động bình thường |
| 95 | UsersController | AddRole | UsersController_AddRole | Thêm role cho user | UserId hợp lệ, Role hợp lệ, UserService hoạt động bình thường |
| 96 | UsersController | RemoveRole | UsersController_RemoveRole | Xóa role của user | UserId hợp lệ, Role hợp lệ, UserService hoạt động bình thường |
| 97 | UsersController | SearchUsers | UsersController_SearchUsers | Tìm kiếm users theo search term | SearchTerm hợp lệ, UserService hoạt động bình thường |
| 98 | UsersController | ValidateCredentials | UsersController_ValidateCredentials | Xác thực credentials của user | Email và Password hợp lệ, UserService hoạt động bình thường |
| 99 | WatchlistController | AddWatchList | WatchlistController_AddWatchList | Thêm auction vào watchlist | UserId và AuctionId hợp lệ, Auction chưa có trong watchlist, WatchlistService hoạt động bình thường |
| 100 | WatchlistController | RemoveWatchList | WatchlistController_RemoveWatchList | Xóa auction khỏi watchlist | UserId và AuctionId hợp lệ, Auction đã có trong watchlist, WatchlistService hoạt động bình thường |
| 101 | WatchlistController | GetByUser | WatchlistController_GetByUser | Lấy danh sách watchlist của user | UserId hợp lệ, WatchlistService hoạt động bình thường |
| 102 | WatchlistController | GetDetail | WatchlistController_GetDetail | Lấy thông tin chi tiết watchlist item | WatchlistId hợp lệ, Watchlist tồn tại, WatchlistService hoạt động bình thường |
| 103 | WatchlistController | GetDetailByUserAuction | WatchlistController_GetDetailByUserAuction | Lấy thông tin watchlist item theo user và auction | UserId và AuctionId hợp lệ, WatchlistService hoạt động bình thường |

---

## Summary

- **Total Modules:** 19 Controllers
- **Total Methods:** 103 Methods
- **Total Test Cases:** 247 Test Cases
- **Test Coverage:** 100% of controllers have test coverage

---

**Prepared By:** [Tên người chuẩn bị]  
**Reviewed By:** [Tên người review]  
**Approved By:** [Tên người phê duyệt]  
**Date:** 2025-12-06



