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
    private readonly AuctionsController _controller;

    public AuctionsControllerTests()
    {
        _auctionServiceMock = new Mock<IAuctionService>();
        _bidServiceMock = new Mock<IBidService>();
        _hubContextMock = new Mock<IHubContext<AuctionHub>>();
        _loggerMock = new Mock<ILogger<AuctionsController>>();
        _vectorSyncServiceMock = new Mock<IVectorSyncService>();
        _itemServiceMock = new Mock<IItemService>();
        _controller = new AuctionsController(
            _auctionServiceMock.Object,
            _bidServiceMock.Object,
            _hubContextMock.Object,
           _loggerMock.Object,
           _vectorSyncServiceMock.Object,
           _itemServiceMock.Object);
    }

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
}

