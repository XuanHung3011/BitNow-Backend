# BitNow-Backend.Tests

Project test cho BitNow-Backend API, sử dụng xUnit, Moq và FluentAssertions.

## Cấu trúc

Project test được tổ chức theo cấu trúc controllers:

```
BitNow-Backend.Tests/
├── Controllers/
│   ├── AuthControllerTests.cs
│   ├── AuctionsControllerTests.cs
│   ├── CategoriesControllerTests.cs
│   ├── MessagesControllerTests.cs
│   ├── AdminAuctionsControllerTests.cs
│   ├── AdminStatsControllerTests.cs
│   ├── AuctionMessagesControllerTests.cs
│   └── UsersControllerTests.cs
└── README.md
```

## Công nghệ sử dụng

- **xUnit**: Framework test
- **Moq**: Mocking framework cho dependencies
- **FluentAssertions**: Assertions dễ đọc hơn
- **Microsoft.AspNetCore.Mvc.Testing**: Testing utilities cho ASP.NET Core

## Chạy tests

### Chạy tất cả tests:
```bash
dotnet test
```

### Chạy tests với output chi tiết:
```bash
dotnet test --verbosity normal
```

### Chạy tests cho một class cụ thể:
```bash
dotnet test --filter "FullyQualifiedName~AuthControllerTests"
```

## Test Coverage

Hiện tại project có **82 tests** cho các controllers:

- **AuthController**: 11 tests
  - Register (valid/invalid/duplicate email)
  - Login (valid/invalid/non-existent user)
  - Verify email
  - Forgot password
  - Reset password

- **AuctionsController**: 12 tests
  - Create auction
  - Get auction details
  - Place bid
  - Get recent bids
  - Get highest bid
  - Get active bids by buyer
  - Get won auctions
  - Get bidding history

- **CategoriesController**: 15 tests
  - Get all categories
  - Get category by ID/slug
  - Create category
  - Update category
  - Delete category
  - Check slug/name exists
  - Is category in use

- **UsersController**: 15 tests
  - Get users (all/by ID/by email)
  - Create user
  - Update user
  - Change password
  - Activate/Deactivate user
  - Add/Remove role
  - Search users
  - Validate credentials

- **MessagesController**: 15 tests
  - Send message (valid/invalid/null/ArgumentException/Exception)
  - Get conversations
  - Get conversation (with/without auctionId)
  - Mark as read (valid/not found/Exception)
  - Get unread messages
  - Get all messages

- **AdminAuctionsController**: 16 tests
  - GetAuctions (valid params/invalid sortBy/invalid sortOrder/invalid statuses/Exception)
  - GetAuctionDetail (existing/not found/Exception)
  - UpdateStatus (missing status/invalid status/not found/cancelled invalid reason/cancelled invalid signature/valid completed/ArgumentException)
  - ResumeAuction (not found/status not cancelled/end time passed/valid/Exception)

- **AdminStatsController**: 4 tests
  - GetAdminStats (success/Exception)
  - GetAdminStatsDetail (success/Exception)

- **AuctionMessagesController**: 6 tests
  - GetMessages (success/ArgumentException/Exception)
  - CreateMessage (success/ArgumentException/Exception)

## Lưu ý

- **ItemsController**: Chưa có tests do conflict namespace với IFileUploadService (có trong cả API và BLL projects). Có thể cần refactor để giải quyết vấn đề này.
- **Đã giải quyết vấn đề trên (xoá IFileUploadService ở BLL projects)
## Best Practices

1. **Arrange-Act-Assert (AAA)**: Tất cả tests đều tuân theo pattern AAA
2. **Mocking**: Sử dụng Moq để mock tất cả dependencies
3. **Isolation**: Mỗi test độc lập, không phụ thuộc vào test khác
4. **Naming**: Test methods có tên mô tả rõ ràng: `MethodName_Scenario_ExpectedResult`

## Thêm tests mới

Khi thêm tests mới:

1. Tạo test class trong thư mục `Controllers/`
2. Mock tất cả dependencies cần thiết
3. Sử dụng FluentAssertions cho assertions
4. Đảm bảo test có tên mô tả rõ ràng
5. Chạy `dotnet test` để đảm bảo tất cả tests pass

