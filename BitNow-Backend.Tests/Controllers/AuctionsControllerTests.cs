using BitNow_Backend.BLL.IServices;
using BitNow_Backend.Controllers;
using BitNow_Backend.DAL.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using BitNow_Backend.RealTime;

namespace BitNow_Backend.Tests.Controllers;

public class AuctionsControllerTests
{
    private readonly Mock<IAuctionService> _auctionServiceMock;
    private readonly Mock<IBidService> _bidServiceMock;
    private readonly Mock<IHubContext<AuctionHub>> _hubContextMock;
    private readonly Mock<ILogger<AuctionsController>> _loggerMock;
    private readonly Mock<IVectorSyncService> _vectorSyncServiceMock;
    private readonly Mock<IItemService> _itemServiceMock;
    private readonly Mock<IUserAuctionViewService> _userAuctionViewServiceMock;
    private readonly AuctionsController _controller;

    public AuctionsControllerTests()
    {
        _auctionServiceMock = new Mock<IAuctionService>();
        _bidServiceMock = new Mock<IBidService>();
        _hubContextMock = new Mock<IHubContext<AuctionHub>>();
        _loggerMock = new Mock<ILogger<AuctionsController>>();
        _vectorSyncServiceMock = new Mock<IVectorSyncService>();
        _itemServiceMock = new Mock<IItemService>();
        _userAuctionViewServiceMock = new Mock<IUserAuctionViewService>();
        _controller = new AuctionsController(
            _auctionServiceMock.Object,
            _bidServiceMock.Object,
            _hubContextMock.Object,
            _loggerMock.Object,
            _vectorSyncServiceMock.Object,
            _itemServiceMock.Object,
            _userAuctionViewServiceMock.Object);
    }

    /// <summary>
    /// Test ID: AUCTION-01
    /// Precondition: Item tồn tại và đã approved, Seller hợp lệ, AuctionService hoạt động bình thường, SignalR HubContext hoạt động
    /// Input: CreateAuctionDto hợp lệ (ItemId=1, SellerId=1, StartingBid=100, BuyNowPrice=500, StartTime/EndTime hợp lệ)
    /// Condition: Tạo auction mới với thông tin hợp lệ và broadcast qua SignalR
    /// Confirmation: HTTP 201 Created, trả về AuctionResponseDto với Id=1, Status="active"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng tạo auction thành công
    /// </summary>
    [Fact]
    public async Task Create_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateAuctionDto
        {
            ItemId = 1,
            SellerId = 1,
            StartingBid = 100,
            BuyNowPrice = 500,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddDays(7)
        };

        var createdAuction = new AuctionResponseDto
        {
            Id = 1,
            ItemId = 1,
            SellerId = 1,
            StartingBid = 100,
            Status = "active"
        };

        _auctionServiceMock.Setup(x => x.CreateAuctionAsync(createDto))
            .ReturnsAsync(createdAuction);

        // Mock HubContext clients
        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);
        _hubContextMock.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdAuction);
    }

    /// <summary>
    /// Test ID: AUCTION-02
    /// Precondition: Dữ liệu không hợp lệ (ItemId=0, SellerId=0, StartingBid < 0)
    /// Input: CreateAuctionDto với dữ liệu không hợp lệ
    /// Condition: Service throw ArgumentException (Invalid auction data)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp dữ liệu không hợp lệ
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateAuctionDto
        {
            ItemId = 0,
            SellerId = 0,
            StartingBid = -100
        };

        _auctionServiceMock.Setup(x => x.CreateAuctionAsync(createDto))
            .ThrowsAsync(new ArgumentException("Invalid auction data"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-03
    /// Precondition: Auction tồn tại trong hệ thống, AuctionService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Lấy chi tiết auction theo ID hợp lệ
    /// Confirmation: HTTP 200 OK, trả về AuctionDetailDto với Id=1, ItemTitle="Test Item"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy chi tiết auction thành công
    /// </summary>
    [Fact]
    public async Task Get_WithValidId_ReturnsOk()
    {
        // Arrange
        var auctionDetail = new AuctionDetailDto
        {
            Id = 1,
            ItemId = 1,
            ItemTitle = "Test Item",
            StartingBid = 100,
            Status = "active"
        };

        _auctionServiceMock.Setup(x => x.GetDetailAsync(1))
            .ReturnsAsync(auctionDetail);

        // Act
        var result = await _controller.Get(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(auctionDetail);
    }

    /// <summary>
    /// Test ID: AUCTION-04
    /// Precondition: Auction không tồn tại trong hệ thống
    /// Input: AuctionId không tồn tại (999)
    /// Condition: Lấy chi tiết auction với ID không tồn tại
    /// Confirmation: HTTP 404 NotFound
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp auction không tồn tại
    /// </summary>
    [Fact]
    public async Task Get_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetDetailAsync(999))
            .ReturnsAsync((AuctionDetailDto?)null);

        // Act
        var result = await _controller.Get(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-05
    /// Precondition: Auction tồn tại và đang active, Bidder hợp lệ, Bid amount > current bid, BidService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1), BidRequestDto hợp lệ (BidderId=1, Amount=150)
    /// Condition: Đặt bid với số tiền hợp lệ và cao hơn current bid
    /// Confirmation: HTTP 200 OK, trả về BidResultDto với CurrentBid=150, BidCount=1
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng đặt bid thành công
    /// </summary>
    [Fact]
    public async Task PlaceBid_WithValidData_ReturnsOk()
    {
        // Arrange
        var bidRequest = new BidRequestDto
        {
            BidderId = 1,
            Amount = 150
        };

        var bidResult = new BidResultDto
        {
            AuctionId = 1,
            CurrentBid = 150,
            BidCount = 1,
            PlacedBid = new BidDto
            {
                BidderId = 1,
                Amount = 150,
                BidTime = DateTime.UtcNow
            }
        };

        _bidServiceMock.Setup(x => x.PlaceBidAsync(1, bidRequest.BidderId, bidRequest.Amount, It.IsAny<bool>()))
            .ReturnsAsync(bidResult);

        // Act
        var result = await _controller.PlaceBid(1, bidRequest);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(bidResult);
    }

    /// <summary>
    /// Test ID: AUCTION-06
    /// Precondition: Bid amount <= current bid hoặc không hợp lệ
    /// Input: AuctionId hợp lệ (1), BidRequestDto với Amount=50 (thấp hơn current bid)
    /// Condition: Service throw InvalidOperationException (Bid amount must be higher than current bid)
    /// Confirmation: HTTP 400 BadRequest, thông báo lỗi từ service
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý trường hợp bid amount không hợp lệ
    /// </summary>
    [Fact]
    public async Task PlaceBid_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var bidRequest = new BidRequestDto
        {
            BidderId = 1,
            Amount = 50 // Less than current bid
        };

        _bidServiceMock.Setup(x => x.PlaceBidAsync(1, bidRequest.BidderId, bidRequest.Amount, It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("Bid amount must be higher than current bid"));

        // Act
        var result = await _controller.PlaceBid(1, bidRequest);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-07
    /// Precondition: Auction tồn tại và có BuyNowPrice, Buyer hợp lệ, AuctionService hoạt động bình thường, SignalR HubContext hoạt động
    /// Input: AuctionId hợp lệ (1), BuyNowRequestDto hợp lệ (BuyerId=2)
    /// Condition: Mua ngay auction với BuyNowPrice và broadcast completion qua SignalR
    /// Confirmation: HTTP 200 OK, trả về AuctionCompletionResultDto với Status="completed", CompletionType="buy-now"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng mua ngay auction thành công
    /// </summary>
    [Fact]
    public async Task BuyNow_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new BuyNowRequestDto { BuyerId = 2 };
        var completion = new AuctionCompletionResultDto
        {
            AuctionId = 1,
            WinnerId = 2,
            FinalPrice = 500,
            Status = "completed",
            CompletionType = "buy-now",
            CompletedAt = DateTime.UtcNow
        };

        _auctionServiceMock.Setup(x => x.BuyNowAsync(1, request.BuyerId))
            .ReturnsAsync(completion);

        var mockClients = new Mock<IHubClients>();
        var mockGroup = new Mock<IClientProxy>();
        _hubContextMock.Setup(x => x.Clients).Returns(mockClients.Object);
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockGroup.Object);
        mockGroup.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.BuyNow(1, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(completion);
    }

    /// <summary>
    /// Test ID: AUCTION-08
    /// Precondition: BuyerId không hợp lệ (BuyerId = 0)
    /// Input: AuctionId hợp lệ (1), BuyNowRequestDto với BuyerId=0
    /// Condition: Mua ngay với BuyerId không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo BuyerId is required
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation BuyerId phải > 0
    /// </summary>
    [Fact]
    public async Task BuyNow_WithMissingBuyer_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.BuyNow(1, new BuyNowRequestDto { BuyerId = 0 });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-09
    /// Precondition: Auction tồn tại trong hệ thống, có bids, BidService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1), limit hợp lệ (100)
    /// Condition: Lấy danh sách recent bids của auction
    /// Confirmation: HTTP 200 OK, trả về danh sách BidDto sắp xếp theo thời gian gần nhất
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy recent bids thành công
    /// </summary>
    [Fact]
    public async Task GetRecentBids_ReturnsOk()
    {
        // Arrange
        var bids = new List<BidDto>
        {
            new BidDto { BidderId = 1, Amount = 150, BidTime = DateTime.UtcNow },
            new BidDto { BidderId = 2, Amount = 200, BidTime = DateTime.UtcNow }
        };

        _bidServiceMock.Setup(x => x.GetRecentBidsAsync(1, 100))
            .ReturnsAsync(bids);

        // Act
        var result = await _controller.GetRecentBids(1, 100);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(bids);
    }

    /// <summary>
    /// Test ID: AUCTION-10
    /// Precondition: Auction tồn tại trong hệ thống, có bids, BidService hoạt động bình thường
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Lấy highest bid của auction
    /// Confirmation: HTTP 200 OK, trả về decimal value = 200m (highest bid amount)
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy highest bid thành công
    /// </summary>
    [Fact]
    public async Task GetHighestBid_ReturnsOk()
    {
        // Arrange
        _bidServiceMock.Setup(x => x.GetHighestBidAsync(1))
            .ReturnsAsync(200m);

        // Act
        var result = await _controller.GetHighestBid(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().Be(200m);
    }

    /// <summary>
    /// Test ID: AUCTION-11
    /// Precondition: Buyer tồn tại trong hệ thống, có active bids, AuctionService hoạt động bình thường
    /// Input: BuyerId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy danh sách active bids của buyer có phân trang
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với BuyerActiveBidDto, IsLeading được set đúng
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy active bids của buyer thành công
    /// </summary>
    [Fact]
    public async Task GetActiveBidsByBuyer_ReturnsOk()
    {
        // Arrange
        var paginatedResult = new PaginatedResult<BuyerActiveBidDto>
        {
            Data = new List<BuyerActiveBidDto>
            {
                new BuyerActiveBidDto
                {
                    AuctionId = 1,
                    ItemTitle = "Test Item",
                    CurrentBid = 150,
                    YourHighestBid = 150,
                    IsLeading = true
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetActiveBidsByBuyerAsync(1, 1, 10))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetActiveBidsByBuyer(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-12
    /// Precondition: Buyer tồn tại trong hệ thống, có won auctions, AuctionService hoạt động bình thường
    /// Input: BuyerId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy danh sách won auctions của buyer có phân trang
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với BuyerWonAuctionDto, Status="completed"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy won auctions của buyer thành công
    /// </summary>
    [Fact]
    public async Task GetWonAuctionsByBuyer_ReturnsOk()
    {
        // Arrange
        var paginatedResult = new PaginatedResult<BuyerWonAuctionDto>
        {
            Data = new List<BuyerWonAuctionDto>
            {
                new BuyerWonAuctionDto
                {
                    AuctionId = 1,
                    ItemTitle = "Test Item",
                    FinalBid = 200,
                    Status = "completed"
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetWonAuctionsByBuyerAsync(1, 1, 10))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetWonAuctionsByBuyer(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-13
    /// Precondition: Buyer tồn tại trong hệ thống, có bidding history, BidService hoạt động bình thường
    /// Input: BuyerId hợp lệ (1), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy lịch sử bidding của buyer có phân trang
    /// Confirmation: HTTP 200 OK, trả về PaginatedResultB với BiddingHistoryDto, Status="leading" hoặc "outbid"
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy bidding history thành công
    /// </summary>
    [Fact]
    public async Task GetBiddingHistory_ReturnsOk()
    {
        // Arrange
        var paginatedResult = new PaginatedResultB<BiddingHistoryDto>
        {
            Data = new List<BiddingHistoryDto>
            {
                new BiddingHistoryDto
                {
                    BidId = 1,
                    AuctionId = 1,
                    ItemTitle = "Test Item",
                    YourBid = 150,
                    Status = "leading"
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _bidServiceMock.Setup(x => x.GetBiddingHistoryAsync(1, 1, 10))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetBiddingHistory(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-14
    /// Precondition: CreateAuctionDto là null
    /// Input: null
    /// Condition: Tạo auction với dto null
    /// Confirmation: HTTP 400 BadRequest, thông báo Request body is required
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation dto không được null
    /// </summary>
    [Fact]
    public async Task Create_WithNullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Create(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-15
    /// Precondition: Service throw UnauthorizedAccessException
    /// Input: CreateAuctionDto hợp lệ
    /// Condition: Service throw UnauthorizedAccessException (seller không có quyền)
    /// Confirmation: HTTP 401 Unauthorized
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý UnauthorizedAccessException
    /// </summary>
    [Fact]
    public async Task Create_WithUnauthorizedAccess_ReturnsUnauthorized()
    {
        // Arrange
        var createDto = new CreateAuctionDto
        {
            ItemId = 1,
            SellerId = 1,
            StartingBid = 100
        };

        _auctionServiceMock.Setup(x => x.CreateAuctionAsync(createDto))
            .ThrowsAsync(new UnauthorizedAccessException("Unauthorized"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-16
    /// Precondition: Service throw Exception
    /// Input: CreateAuctionDto hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task Create_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateAuctionDto
        {
            ItemId = 1,
            SellerId = 1,
            StartingBid = 100
        };

        _auctionServiceMock.Setup(x => x.CreateAuctionAsync(createDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test ID: AUCTION-17
    /// Precondition: Auction không có bids
    /// Input: AuctionId hợp lệ (1), limit hợp lệ (100)
    /// Condition: Lấy recent bids của auction không có bids
    /// Confirmation: HTTP 200 OK, trả về danh sách rỗng
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý khi auction không có bids
    /// </summary>
    [Fact]
    public async Task GetRecentBids_WithNoBids_ReturnsEmptyList()
    {
        // Arrange
        _bidServiceMock.Setup(x => x.GetRecentBidsAsync(1, 100))
            .ReturnsAsync(new List<BidDto>());

        // Act
        var result = await _controller.GetRecentBids(1, 100);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeAssignableTo<IReadOnlyList<BidDto>>();
    }

    /// <summary>
    /// Test ID: AUCTION-18
    /// Precondition: limit vượt quá giới hạn (limit > 100)
    /// Input: AuctionId hợp lệ (1), limit=200
    /// Condition: Lấy recent bids với limit vượt quá giới hạn
    /// Confirmation: HTTP 200 OK, limit được clamp về 100
    /// Type: Boundary
    /// Test Requirement: Kiểm tra validation limit được clamp về max 100
    /// </summary>
    [Fact]
    public async Task GetRecentBids_WithLimitExceedingMax_ClampsToMax()
    {
        // Arrange
        var bids = new List<BidDto>();
        _bidServiceMock.Setup(x => x.GetRecentBidsAsync(1, 100))
            .ReturnsAsync(bids);

        // Act
        var result = await _controller.GetRecentBids(1, 200);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _bidServiceMock.Verify(x => x.GetRecentBidsAsync(1, 100), Times.Once);
    }

    /// <summary>
    /// Test ID: AUCTION-19
    /// Precondition: Auction không có bids
    /// Input: AuctionId hợp lệ (1)
    /// Condition: Lấy highest bid của auction không có bids
    /// Confirmation: HTTP 200 OK, trả về null
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý khi auction không có bids
    /// </summary>
    [Fact]
    public async Task GetHighestBid_WithNoBids_ReturnsNull()
    {
        // Arrange
        _bidServiceMock.Setup(x => x.GetHighestBidAsync(1))
            .ReturnsAsync((decimal?)null);

        // Act
        var result = await _controller.GetHighestBid(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeNull();
    }

    /// <summary>
    /// Test ID: AUCTION-20
    /// Precondition: Buyer không có active bids
    /// Input: BuyerId hợp lệ (1), page=1, pageSize=10
    /// Condition: Lấy active bids của buyer không có active bids
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với Data rỗng
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý khi buyer không có active bids
    /// </summary>
    [Fact]
    public async Task GetActiveBidsByBuyer_WithNoActiveBids_ReturnsEmptyList()
    {
        // Arrange
        var emptyResult = new PaginatedResult<BuyerActiveBidDto>
        {
            Data = new List<BuyerActiveBidDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetActiveBidsByBuyerAsync(1, 1, 10))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _controller.GetActiveBidsByBuyer(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-21
    /// Precondition: Buyer không có won auctions
    /// Input: BuyerId hợp lệ (1), page=1, pageSize=10
    /// Condition: Lấy won auctions của buyer không có won auctions
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với Data rỗng
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý khi buyer không có won auctions
    /// </summary>
    [Fact]
    public async Task GetWonAuctionsByBuyer_WithNoWonAuctions_ReturnsEmptyList()
    {
        // Arrange
        var emptyResult = new PaginatedResult<BuyerWonAuctionDto>
        {
            Data = new List<BuyerWonAuctionDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetWonAuctionsByBuyerAsync(1, 1, 10))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _controller.GetWonAuctionsByBuyer(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-22
    /// Precondition: Buyer không có bidding history
    /// Input: BuyerId hợp lệ (1), page=1, pageSize=10
    /// Condition: Lấy bidding history của buyer không có history
    /// Confirmation: HTTP 200 OK, trả về PaginatedResultB với Data rỗng
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý khi buyer không có bidding history
    /// </summary>
    [Fact]
    public async Task GetBiddingHistory_WithNoHistory_ReturnsEmptyList()
    {
        // Arrange
        var emptyResult = new PaginatedResultB<BiddingHistoryDto>
        {
            Data = new List<BiddingHistoryDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _bidServiceMock.Setup(x => x.GetBiddingHistoryAsync(1, 1, 10))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _controller.GetBiddingHistory(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-23
    /// Precondition: Service throw Exception khi lấy active bids
    /// Input: BuyerId hợp lệ (1), page=1, pageSize=10
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetActiveBidsByBuyer_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetActiveBidsByBuyerAsync(1, 1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetActiveBidsByBuyer(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test ID: AUCTION-24
    /// Precondition: Service throw Exception khi lấy won auctions
    /// Input: BuyerId hợp lệ (1), page=1, pageSize=10
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetWonAuctionsByBuyer_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetWonAuctionsByBuyerAsync(1, 1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetWonAuctionsByBuyer(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test ID: AUCTION-25
    /// Precondition: Service throw Exception khi lấy bidding history
    /// Input: BuyerId hợp lệ (1), page=1, pageSize=10
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetBiddingHistory_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _bidServiceMock.Setup(x => x.GetBiddingHistoryAsync(1, 1, 10))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBiddingHistory(1, 1, 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test ID: AUCTION-26
    /// Precondition: Service throw Exception khi mua ngay
    /// Input: AuctionId hợp lệ (1), BuyNowRequestDto hợp lệ
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task BuyNow_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new BuyNowRequestDto { BuyerId = 2 };
        _auctionServiceMock.Setup(x => x.BuyNowAsync(1, request.BuyerId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.BuyNow(1, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test ID: AUCTION-27
    /// Precondition: Seller tồn tại trong hệ thống, có auctions, AuctionService hoạt động bình thường
    /// Input: SellerId hợp lệ (1)
    /// Condition: Lấy danh sách auctions của seller
    /// Confirmation: HTTP 200 OK, trả về List<SellerAuctionDto>
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy auctions của seller thành công
    /// </summary>
    [Fact]
    public async Task GetAuctionsBySeller_WithValidSellerId_ReturnsOk()
    {
        // Arrange
        var auctions = new List<SellerAuctionDto>
        {
            new SellerAuctionDto
            {
                Id = 1,
                ItemTitle = "Test Item",
                Status = "active",
                CurrentBid = 150
            }
        };

        _auctionServiceMock.Setup(x => x.GetAuctionsBySellerAsync(1))
            .ReturnsAsync(auctions);

        // Act
        var result = await _controller.GetAuctionsBySeller(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(auctions);
    }

    /// <summary>
    /// Test ID: AUCTION-28
    /// Precondition: Seller không có auctions
    /// Input: SellerId hợp lệ (1)
    /// Condition: Lấy auctions của seller không có auctions
    /// Confirmation: HTTP 200 OK, trả về danh sách rỗng
    /// Type: Boundary
    /// Test Requirement: Kiểm tra xử lý khi seller không có auctions
    /// </summary>
    [Fact]
    public async Task GetAuctionsBySeller_WithNoAuctions_ReturnsEmptyList()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetAuctionsBySellerAsync(1))
            .ReturnsAsync(new List<SellerAuctionDto>());

        // Act
        var result = await _controller.GetAuctionsBySeller(1);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-29
    /// Precondition: Service throw Exception khi lấy auctions của seller
    /// Input: SellerId hợp lệ (1)
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetAuctionsBySeller_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetAuctionsBySellerAsync(1))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAuctionsBySeller(1);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    /// <summary>
    /// Test ID: AUCTION-30
    /// Precondition: AuctionService hoạt động bình thường, có auctions trong hệ thống
    /// Input: searchTerm hợp lệ ("item"), statuses hợp lệ ("active"), sortBy hợp lệ ("EndTime"), sortOrder hợp lệ ("desc"), page hợp lệ (1), pageSize hợp lệ (10)
    /// Condition: Lấy danh sách auctions với filter và sort hợp lệ
    /// Confirmation: HTTP 200 OK, trả về PaginatedResult với AuctionListItemDto
    /// Type: Normal
    /// Test Requirement: Kiểm tra chức năng lấy danh sách auctions với filter thành công
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithValidParams_ReturnsOk()
    {
        // Arrange
        var expectedResult = new PaginatedResult<AuctionListItemDto>
        {
            Data = new List<AuctionListItemDto>
            {
                new()
                {
                    Id = 1,
                    ItemTitle = "Test Item",
                    Status = "active"
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetAuctionsWithFilterAsync(It.IsAny<AuctionFilterDto>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllAuctions(
            searchTerm: "item",
            statuses: "active",
            sortBy: "EndTime",
            sortOrder: "desc",
            page: 1,
            pageSize: 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-31
    /// Precondition: sortBy không hợp lệ
    /// Input: sortBy="InvalidField"
    /// Condition: Lấy auctions với sortBy không được phép
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid sortBy field
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation sortBy chỉ cho phép các field hợp lệ
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithInvalidSortBy_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAllAuctions(sortBy: "InvalidField");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-32
    /// Precondition: sortOrder không hợp lệ
    /// Input: sortOrder="invalid"
    /// Condition: Lấy auctions với sortOrder không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo sortOrder must be 'asc' or 'desc'
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation sortOrder
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithInvalidSortOrder_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAllAuctions(sortOrder: "invalid");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-33
    /// Precondition: statuses không hợp lệ
    /// Input: statuses="invalid_status"
    /// Condition: Lấy auctions với status không hợp lệ
    /// Confirmation: HTTP 400 BadRequest, thông báo Invalid status values
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra validation statuses
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithInvalidStatuses_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetAllAuctions(statuses: "invalid_status");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-34
    /// Precondition: page < 1
    /// Input: page=0, pageSize=10
    /// Condition: Lấy auctions với page không hợp lệ
    /// Confirmation: HTTP 200 OK, page được tự động set về 1
    /// Type: Boundary
    /// Test Requirement: Kiểm tra validation page được tự động set về 1 nếu < 1
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithInvalidPage_ClampsToMin()
    {
        // Arrange
        var expectedResult = new PaginatedResult<AuctionListItemDto>
        {
            Data = new List<AuctionListItemDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetAuctionsWithFilterAsync(It.Is<AuctionFilterDto>(f => f.Page == 1)))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllAuctions(page: 0, pageSize: 10);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-35
    /// Precondition: pageSize > 100
    /// Input: page=1, pageSize=200
    /// Condition: Lấy auctions với pageSize vượt quá giới hạn
    /// Confirmation: HTTP 200 OK, pageSize được tự động set về 10
    /// Type: Boundary
    /// Test Requirement: Kiểm tra validation pageSize được tự động set về 10 nếu > 100
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithInvalidPageSize_ClampsToMax()
    {
        // Arrange
        var expectedResult = new PaginatedResult<AuctionListItemDto>
        {
            Data = new List<AuctionListItemDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _auctionServiceMock.Setup(x => x.GetAuctionsWithFilterAsync(It.Is<AuctionFilterDto>(f => f.PageSize == 10)))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetAllAuctions(page: 1, pageSize: 200);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Test ID: AUCTION-36
    /// Precondition: Service throw Exception khi lấy tất cả auctions
    /// Input: searchTerm hợp lệ, page=1, pageSize=10
    /// Condition: Service throw Exception (database error)
    /// Confirmation: HTTP 500 InternalServerError
    /// Type: Abnormal
    /// Test Requirement: Kiểm tra xử lý exception từ service
    /// </summary>
    [Fact]
    public async Task GetAllAuctions_WithException_ReturnsInternalServerError()
    {
        // Arrange
        _auctionServiceMock.Setup(x => x.GetAuctionsWithFilterAsync(It.IsAny<AuctionFilterDto>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllAuctions(page: 1, pageSize: 10);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}

