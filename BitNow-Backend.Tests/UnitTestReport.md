# Unit Test Case Document - Test Report

## Document Information

**Project Name:** BitNow-Backend  
**Document Type:** Unit Test Case Specification  
**Document Title:** Unit Test Case Document - Test Report  
**Version:** 1.0  
**Date:** 2025-01-20

---

## Test Report Summary

| No | Function Code | Passed | Failed | Untested | Normal Case | Abnormal Case | Boundary Case | Total Test Cases |
|----|---------------|--------|--------|----------|-------------|---------------|---------------|------------------|
| 1 | AuthController.Register | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 2 | AuthController.Login | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 3 | AuthController.Verify | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 4 | AuthController.ForgotPassword | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 5 | AuthController.ResetPassword | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 6 | AuthController.Resend | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 7 | AuctionsController.Create | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 8 | AuctionsController.Get | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 9 | AuctionsController.PlaceBid | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 10 | AuctionsController.BuyNow | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 11 | AuctionsController.GetRecentBids | 3 | 0 | 0 | 1 | 0 | 2 | 3 |
| 12 | AuctionsController.GetHighestBid | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| 13 | AuctionsController.GetActiveBidsByBuyer | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| 14 | AuctionsController.GetWonAuctionsByBuyer | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| 15 | AuctionsController.GetBiddingHistory | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| 16 | AuctionsController.GetAuctionsBySeller | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| 17 | AuctionsController.GetAllAuctions | 7 | 0 | 0 | 1 | 4 | 2 | 7 |
| 18 | AdminAuctionsController.GetAuctions | 5 | 0 | 0 | 1 | 4 | 0 | 5 |
| 19 | AdminAuctionsController.GetAuctionDetail | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 20 | AdminAuctionsController.UpdateStatus | 7 | 0 | 0 | 1 | 6 | 0 | 7 |
| 21 | AdminAuctionsController.ResumeAuction | 5 | 0 | 0 | 1 | 4 | 0 | 5 |
| 22 | AdminStatsController.GetAdminStats | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 23 | AdminStatsController.GetAdminStatsDetail | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 24 | AuctionMessagesController.GetMessages | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 25 | AuctionMessagesController.CreateMessage | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 26 | AutoBidsController.CreateOrUpdate | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 27 | AutoBidsController.Get | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 28 | AutoBidsController.Deactivate | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 29 | AutoBidsController.GetBidIncrement | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| 30 | CategoriesController.GetAllCategories | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 31 | CategoriesController.GetCategory | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 32 | CategoriesController.GetCategoryBySlug | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 33 | CategoriesController.CreateCategory | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 34 | CategoriesController.UpdateCategory | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 35 | CategoriesController.DeleteCategory | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 36 | CategoriesController.GetCategoriesPaged | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 37 | CategoriesController.CheckSlugExists | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 38 | CategoriesController.IsCategoryInUse | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 39 | FavoriteSellersController.GetMyFavorites | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 40 | FavoriteSellersController.CheckIsFavorite | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 41 | FavoriteSellersController.AddFavorite | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 42 | FavoriteSellersController.RemoveFavorite | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 43 | HomeController.GetAllItems | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 44 | HomeController.GetAllItemsPaged | 4 | 0 | 0 | 1 | 1 | 2 | 4 |
| 45 | HomeController.SearchItems | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 46 | HomeController.SearchItemsPaged | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 47 | HomeController.FilterItems | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 48 | HomeController.GetCategories | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 49 | HomeController.GetHot | 5 | 0 | 0 | 1 | 1 | 3 | 5 |
| 50 | ItemsController.CreateItem | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 51 | ItemsController.CreateDraftItem | 1 | 0 | 0 | 1 | 0 | 0 | 1 |
| 52 | ItemsController.UpdateDraftItem | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 53 | ItemsController.GetAllItems | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 54 | ItemsController.ApproveItem | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 55 | ItemsController.RejectItem | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 56 | ItemsController.GetItemById | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 57 | ItemsController.DeleteItem | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 58 | MessagesController.SendMessage | 5 | 0 | 0 | 1 | 4 | 0 | 5 |
| 59 | MessagesController.GetConversations | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 60 | MessagesController.GetConversation | 3 | 0 | 0 | 2 | 1 | 0 | 3 |
| 61 | MessagesController.MarkAsRead | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 62 | MessagesController.GetUnreadMessages | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 63 | MessagesController.GetAllMessages | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 64 | NotificationsController.GetNotifications | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 65 | NotificationsController.GetUnreadNotifications | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 66 | NotificationsController.GetUnreadCount | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 67 | NotificationsController.CreateNotification | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 68 | NotificationsController.MarkAsRead | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 69 | NotificationsController.MarkAllAsRead | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 70 | NotificationsController.DeleteNotification | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 71 | PaymentController.CreatePaymentLink | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 72 | PaymentController.HandleWebhook | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 73 | PaymentController.GetOrderByAuctionId | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 74 | PaymentController.GetBuyerOrders | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 75 | PaymentController.GetSellerOrders | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 76 | PaymentController.UpdateShippingInfo | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 77 | PaymentController.ConfirmOrderReceived | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 78 | PaymentController.ReportOrderIssue | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 79 | PlatformAnalyticsController.GetPlatformAnalytics | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 80 | PlatformAnalyticsController.GetAnalyticsDetail | 5 | 0 | 0 | 3 | 1 | 1 | 5 |
| 81 | RatingsController.Create | 6 | 0 | 0 | 1 | 3 | 2 | 6 |
| 82 | RatingsController.GetForUser | 5 | 0 | 0 | 1 | 2 | 2 | 5 |
| 83 | RatingsController.GetForAuction | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| 84 | RecommendationsController.GetPersonalized | 5 | 0 | 0 | 1 | 2 | 2 | 5 |
| 85 | RecommendationsController.SyncActiveAuctions | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 86 | RecommendationsController.RemoveExpiredAuctions | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 87 | RecommendationsController.ClearAllVectors | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| 88 | SellerStatsController.GetSellerStats | 3 | 0 | 0 | 2 | 1 | 0 | 3 |
| 89 | SellerStatsController.GetSellerStatsDetail | 5 | 0 | 0 | 3 | 1 | 1 | 5 |
| 90 | UsersController.GetUsers | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 91 | UsersController.GetUser | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 92 | UsersController.GetUserByEmail | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 93 | UsersController.CreateUser | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 94 | UsersController.UpdateUser | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 95 | UsersController.ChangePassword | 6 | 0 | 0 | 1 | 5 | 0 | 6 |
| 96 | UsersController.ActivateUser | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 97 | UsersController.DeactivateUser | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 98 | UsersController.AddRole | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 99 | UsersController.RemoveRole | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 100 | UsersController.SearchUsers | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 101 | UsersController.ValidateCredentials | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 102 | WatchlistController.AddWatchList | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 103 | WatchlistController.RemoveWatchList | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| 104 | WatchlistController.GetByUser | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 105 | WatchlistController.GetDetail | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| 106 | WatchlistController.GetDetailByUserAuction | 3 | 0 | 0 | 1 | 2 | 0 | 3 |
| **TOTAL** | **106 Functions** | **246** | **0** | **0** | **103** | **127** | **16** | **246** |

---

## Test Statistics Summary

### Overall Statistics

| Metric | Count | Percentage |
|--------|-------|------------|
| **Total Functions** | 106 | 100% |
| **Total Test Cases** | 246 | 100% |
| **Passed Test Cases** | 246 | 100% |
| **Failed Test Cases** | 0 | 0% |
| **Untested Test Cases** | 0 | 0% |
| **Normal Test Cases** | 103 | 41.9% |
| **Abnormal Test Cases** | 127 | 51.6% |
| **Boundary Test Cases** | 16 | 6.5% |

### Test Coverage

**Test Coverage:** 100%  
*(Tất cả 106 functions đều có test cases)*

**Test Successful Coverage:** 100%  
*(246/246 test cases passed, 0 test cases failed or skipped)*

### Test Case Distribution

**Normal Case:** 41.9% (103/246)  
**Abnormal Case:** 51.6% (127/246)  
**Boundary Case:** 6.5% (16/246)

---

## Detailed Statistics by Controller

| Controller | Functions | Total TC | Passed | Failed | Untested | Normal | Abnormal | Boundary |
|------------|-----------|----------|--------|--------|----------|--------|----------|----------|
| AuthController | 6 | 14 | 14 | 0 | 0 | 6 | 8 | 0 |
| AuctionsController | 11 | 36 | 36 | 0 | 0 | 11 | 16 | 9 |
| AdminAuctionsController | 4 | 20 | 20 | 0 | 0 | 4 | 15 | 1 |
| AdminStatsController | 2 | 4 | 4 | 0 | 0 | 2 | 2 | 0 |
| AuctionMessagesController | 2 | 6 | 6 | 0 | 0 | 2 | 4 | 0 |
| AutoBidsController | 4 | 12 | 12 | 0 | 0 | 4 | 7 | 1 |
| CategoriesController | 9 | 14 | 14 | 0 | 0 | 9 | 5 | 0 |
| FavoriteSellersController | 4 | 10 | 10 | 0 | 0 | 4 | 5 | 1 |
| HomeController | 8 | 22 | 22 | 0 | 0 | 7 | 10 | 5 |
| ItemsController | 8 | 18 | 18 | 0 | 0 | 8 | 10 | 0 |
| MessagesController | 6 | 17 | 17 | 0 | 0 | 7 | 10 | 0 |
| NotificationsController | 7 | 18 | 18 | 0 | 0 | 7 | 11 | 0 |
| PaymentController | 8 | 19 | 19 | 0 | 0 | 8 | 11 | 0 |
| PlatformAnalyticsController | 2 | 7 | 7 | 0 | 0 | 4 | 2 | 1 |
| RatingsController | 3 | 14 | 14 | 0 | 0 | 3 | 6 | 5 |
| RecommendationsController | 4 | 11 | 11 | 0 | 0 | 4 | 5 | 2 |
| SellerStatsController | 2 | 8 | 8 | 0 | 0 | 5 | 2 | 1 |
| UsersController | 12 | 42 | 42 | 0 | 0 | 12 | 30 | 0 |
| WatchlistController | 5 | 17 | 17 | 0 | 0 | 5 | 12 | 0 |
| **TOTAL** | **106** | **246** | **246** | **0** | **0** | **103** | **127** | **16** |

---

## Test Coverage Analysis

### Coverage by Test Type

- **Normal Cases:** 103 test cases (41.9%)
  - Test với giá trị hợp lệ, phổ biến
  - Đảm bảo chức năng hoạt động đúng trong điều kiện bình thường

- **Abnormal Cases:** 127 test cases (51.6%)
  - Test với giá trị không hợp lệ, exception handling
  - Đảm bảo hệ thống xử lý lỗi đúng cách

- **Boundary Cases:** 16 test cases (6.5%)
  - Test với giá trị biên (min, max, edge cases)
  - Đảm bảo validation và edge case handling

### Test Results

- **Passed:** 246 test cases (100%)
  - Tất cả test cases đều pass, đảm bảo chất lượng code

- **Failed:** 0 test cases (0%)
  - Không có test case nào fail

- **Untested:** 0 test cases (0%)
  - Tất cả test cases đều đã được thực thi và pass

---

## Test Coverage Percentage

### Overall Coverage

- **Test Coverage:** 100%
  - Tất cả 106 functions đều có test cases
  - Không có function nào thiếu test coverage

- **Test Successful Coverage:** 100%
  - 246/246 test cases passed
  - 0 test cases failed or skipped

### Coverage by Test Type

- **Normal Case Coverage:** 41.9%
  - 103/246 test cases là Normal cases
  - Đảm bảo test các scenarios bình thường

- **Abnormal Case Coverage:** 51.6%
  - 127/246 test cases là Abnormal cases
  - Tập trung vào exception handling và error cases

- **Boundary Case Coverage:** 6.5%
  - 16/246 test cases là Boundary cases
  - Test các giá trị biên và edge cases

---

## Notes

1. **Test Coverage 100%:** Tất cả 106 functions đều có test cases, đảm bảo coverage đầy đủ.

2. **Test Successful Coverage 100%:** Với 246/246 test cases passed, đây là tỷ lệ hoàn hảo, đảm bảo chất lượng code tốt.

3. **Phân bố Test Cases:** 
   - Abnormal cases chiếm tỷ lệ cao nhất (51.6%), phù hợp với best practice về exception handling
   - Normal cases chiếm 41.9%, đảm bảo test các happy paths
   - Boundary cases chiếm 6.5%, test các edge cases quan trọng

4. **Tất cả test cases đã pass:** Không còn test case nào bị fail hoặc skip, đảm bảo chất lượng code cao.

---

**Prepared By:** [Tên người chuẩn bị]  
**Reviewed By:** [Tên người review]  
**Approved By:** [Tên người phê duyệt]  
**Date:** 2025-12-06
