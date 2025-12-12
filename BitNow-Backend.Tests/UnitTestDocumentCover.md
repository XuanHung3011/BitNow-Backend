# Unit Test Case Document - Cover Page

## Document Information

**Project Name:** BitNow-Backend  
**Document Type:** Unit Test Case Specification  
**Document Title:** Unit Test Case Document  
**Version:** 1.0  
**Date:** 2025-12-06  
**Total Test Cases:** 247  
**Test Pass Rate:** 99.6% (246/247 passed, 1 skipped)

---

## Record of Change

| Effective Date | Version | Change Item | *A,D,M | Change Description (Method Names) | Reference |
|---------------|---------|-------------|--------|----------------------------------|-----------|
| 2025-12-06 | 1.0 | AuthController | A | Register, Login, Verify, ForgotPassword, ResetPassword, Resend | AuthControllerTests.cs |
| 2025-12-06 | 1.0 | AuctionsController | A | Create, Get, PlaceBid, BuyNow, GetRecentBids, GetHighestBid, GetActiveBidsByBuyer, GetWonAuctionsByBuyer, GetBiddingHistory, GetAuctionsBySeller, GetAllAuctions | AuctionsControllerTests.cs |
| 2025-12-06 | 1.0 | AdminAuctionsController | A | GetAuctions, GetAuctionDetail, UpdateStatus, ResumeAuction | AdminAuctionsControllerTests.cs |
| 2025-12-06 | 1.0 | AdminStatsController | A | GetAdminStats, GetAdminStatsDetail | AdminStatsControllerTests.cs |
| 2025-12-06 | 1.0 | AuctionMessagesController | A | GetMessages, CreateMessage | AuctionMessagesControllerTests.cs |
| 2025-12-06 | 1.0 | AutoBidsController | A | CreateOrUpdate, Get, Deactivate, GetBidIncrement | AutoBidsControllerTests.cs |
| 2025-12-06 | 1.0 | CategoriesController | A | GetAllCategories, GetCategory, GetCategoryBySlug, CreateCategory, UpdateCategory, DeleteCategory, GetCategoriesPaged, CheckSlugExists, IsCategoryInUse | CategoriesControllerTests.cs |
| 2025-12-06 | 1.0 | FavoriteSellersController | A | GetMyFavorites, CheckIsFavorite, AddFavorite, RemoveFavorite | FavoriteSellersControllerTests.cs |
| 2025-12-06 | 1.0 | HomeController | A | GetAllItems, GetAllItemsPaged, SearchItems, SearchItemsPaged, FilterItems, GetCategories, GetHot | HomeControllerTests.cs |
| 2025-12-06 | 1.0 | ItemsController | A | CreateItem, CreateDraftItem, UpdateDraftItem, GetAllItems, ApproveItem, RejectItem, GetItemById, DeleteItem | ItemsControllerTests.cs |
| 2025-12-06 | 1.0 | MessagesController | A | SendMessage, GetConversations, GetConversation, MarkAsRead, GetUnreadMessages, GetAllMessages | MessagesControllerTests.cs |
| 2025-12-06 | 1.0 | NotificationsController | A | GetNotifications, GetUnreadNotifications, GetUnreadCount, CreateNotification, MarkAsRead, MarkAllAsRead, DeleteNotification | NotificationsControllerTests.cs |
| 2025-12-06 | 1.0 | PaymentController | A | CreatePaymentLink, HandleWebhook, GetOrderByAuctionId, GetBuyerOrders, GetSellerOrders, UpdateShippingInfo, ConfirmOrderReceived, ReportOrderIssue | PaymentControllerTests.cs |
| 2025-12-06 | 1.0 | PlatformAnalyticsController | A | GetPlatformAnalytics, GetAnalyticsDetail | PlatformAnalyticsControllerTests.cs |
| 2025-12-06 | 1.0 | RatingsController | A | Create, GetForUser, GetForAuction | RatingsControllerTests.cs |
| 2025-12-06 | 1.0 | RecommendationsController | A | GetPersonalized | RecommendationsControllerTests.cs |
| 2025-12-06 | 1.0 | SellerStatsController | A | GetSellerStats, GetSellerStatsDetail | SellerStatsControllerTests.cs |
| 2025-12-06 | 1.0 | UsersController | A | GetUsers, GetUser, GetUserByEmail, CreateUser, UpdateUser, ChangePassword, ActivateUser, DeactivateUser, AddRole, RemoveRole, SearchUsers, ValidateCredentials | UsersControllerTests.cs |
| 2025-12-06 | 1.0 | WatchlistController | A | AddWatchList, RemoveWatchList, GetByUser, GetDetail, GetDetailByUserAuction | WatchlistControllerTests.cs |

---

## Legend

**\*A,D,M:**
- **A** = Add (Thêm mới)
- **D** = Delete (Xóa)
- **M** = Modify (Sửa đổi)

---

## Summary Statistics

| Metric | Value |
|--------|-------|
| Total Controllers | 19 |
| Total Test Classes | 19 |
| Total Test Cases | 247 |
| Total Methods Tested | 80+ |
| Normal Test Cases | 102 |
| Boundary Test Cases | 15 |
| Abnormal Test Cases | 130 |
| Passed Tests | 246 (99.6%) |
| Skipped Tests | 1 (0.4%) |
| Failed Tests | 0 (0%) |
| Total LOC (Controllers) | 3,224 |
| Total KLOC | 3.224 |

---

## Test Coverage by Controller

| Controller | Methods Tested | Test Cases | Normal | Boundary | Abnormal | Status |
|------------|----------------|------------|--------|----------|----------|--------|
| AuthController | 6 | 12 | 5 | 0 | 7 | ✅ Complete |
| AuctionsController | 11 | 36 | 11 | 9 | 16 | ✅ Complete |
| AdminAuctionsController | 4 | 20 | 4 | 1 | 15 | ✅ Complete |
| AdminStatsController | 2 | 4 | 2 | 0 | 2 | ✅ Complete |
| AuctionMessagesController | 2 | 6 | 2 | 0 | 4 | ✅ Complete |
| AutoBidsController | 4 | 12 | 4 | 1 | 7 | ✅ Complete |
| CategoriesController | 9 | 14 | 9 | 0 | 5 | ✅ Complete |
| FavoriteSellersController | 4 | 10 | 4 | 1 | 5 | ✅ Complete |
| HomeController | 7 | 17 | 6 | 2 | 9 | ✅ Complete |
| ItemsController | 8 | 18 | 8 | 0 | 10 | ✅ Complete |
| MessagesController | 6 | 17 | 7 | 0 | 10 | ✅ Complete |
| NotificationsController | 7 | 18 | 7 | 0 | 11 | ✅ Complete |
| PaymentController | 8 | 15 | 8 | 0 | 7 | ✅ Complete (1 skipped) |
| PlatformAnalyticsController | 2 | 4 | 2 | 0 | 2 | ✅ Complete |
| RatingsController | 3 | 8 | 3 | 0 | 5 | ✅ Complete |
| RecommendationsController | 1 | 4 | 1 | 1 | 2 | ✅ Complete |
| SellerStatsController | 2 | 4 | 2 | 0 | 2 | ✅ Complete |
| UsersController | 12 | 16 | 12 | 0 | 4 | ✅ Complete |
| WatchlistController | 5 | 11 | 5 | 0 | 6 | ✅ Complete |

---

## Detailed Method List by Controller

### AuthController (6 methods)
1. Register
2. Login
3. Verify
4. ForgotPassword
5. ResetPassword
6. Resend

### AuctionsController (11 methods)
1. Create
2. Get
3. PlaceBid
4. BuyNow
5. GetRecentBids
6. GetHighestBid
7. GetActiveBidsByBuyer
8. GetWonAuctionsByBuyer
9. GetBiddingHistory
10. GetAuctionsBySeller
11. GetAllAuctions

### AdminAuctionsController (4 methods)
1. GetAuctions
2. GetAuctionDetail
3. UpdateStatus
4. ResumeAuction

### AdminStatsController (2 methods)
1. GetAdminStats
2. GetAdminStatsDetail

### AuctionMessagesController (2 methods)
1. GetMessages
2. CreateMessage

### AutoBidsController (4 methods)
1. CreateOrUpdate
2. Get
3. Deactivate
4. GetBidIncrement

### CategoriesController (9 methods)
1. GetAllCategories
2. GetCategory
3. GetCategoryBySlug
4. CreateCategory
5. UpdateCategory
6. DeleteCategory
7. GetCategoriesPaged
8. CheckSlugExists
9. IsCategoryInUse

### FavoriteSellersController (4 methods)
1. GetMyFavorites
2. CheckIsFavorite
3. AddFavorite
4. RemoveFavorite

### HomeController (7 methods)
1. GetAllItems
2. GetAllItemsPaged
3. SearchItems
4. SearchItemsPaged
5. FilterItems
6. GetCategories
7. GetHot

### ItemsController (8 methods)
1. CreateItem
2. CreateDraftItem
3. UpdateDraftItem
4. GetAllItems
5. ApproveItem
6. RejectItem
7. GetItemById
8. DeleteItem

### MessagesController (6 methods)
1. SendMessage
2. GetConversations
3. GetConversation
4. MarkAsRead
5. GetUnreadMessages
6. GetAllMessages

### NotificationsController (7 methods)
1. GetNotifications
2. GetUnreadNotifications
3. GetUnreadCount
4. CreateNotification
5. MarkAsRead
6. MarkAllAsRead
7. DeleteNotification

### PaymentController (8 methods)
1. CreatePaymentLink
2. HandleWebhook
3. GetOrderByAuctionId
4. GetBuyerOrders
5. GetSellerOrders
6. UpdateShippingInfo
7. ConfirmOrderReceived
8. ReportOrderIssue

### PlatformAnalyticsController (2 methods)
1. GetPlatformAnalytics
2. GetAnalyticsDetail

### RatingsController (3 methods)
1. Create
2. GetForUser
3. GetForAuction

### RecommendationsController (1 method)
1. GetPersonalized

### SellerStatsController (2 methods)
1. GetSellerStats
2. GetSellerStatsDetail

### UsersController (12 methods)
1. GetUsers
2. GetUser
3. GetUserByEmail
4. CreateUser
5. UpdateUser
6. ChangePassword
7. ActivateUser
8. DeactivateUser
9. AddRole
10. RemoveRole
11. SearchUsers
12. ValidateCredentials

### WatchlistController (5 methods)
1. AddWatchList
2. RemoveWatchList
3. GetByUser
4. GetDetail
5. GetDetailByUserAuction

---

## Document Status

✅ **Status:** Complete  
✅ **All controllers have test coverage**  
✅ **All test cases have XML comments with full information**  
✅ **All test cases have unique Test IDs**  
✅ **All test cases are classified as Normal/Boundary/Abnormal**  
✅ **Test results: 246/247 passed (99.6%)**  
✅ **0 warnings, 0 errors**

---

## Reference Documents

1. **Test Files:** All test classes in `BitNow-Backend.Tests/Controllers/` directory
2. **Controller Files:** All controller classes in `BitNow-Backend/Controllers/` directory
3. **Test Coverage Analysis:** TestCoverageAnalysis.csv
4. **Change Description:** ChangeDescription.md (if exists)
5. **Unit Test Template Guide:** Unit Test Case Template Specification

---

**Prepared By:** [Tên người chuẩn bị]  
**Reviewed By:** [Tên người review]  
**Approved By:** [Tên người phê duyệt]  
**Date:** 2025-12-06



