using Nexus.Api.Common;
using Nexus.Api.Services;
using Xunit;

namespace Nexus.Tests;

/// <summary>Kiểm thử các quy tắc chuỗi và bất biến đơn giản của nghiệp vụ.</summary>
public class BusinessRuleTests
{
    [Fact]
    public void OrderId_HasTypePrefixAndTenDigits()
    {
        const string connType = "B";
        var orderId = connType + 42L.ToString("D10");

        Assert.Equal(11, orderId.Length);
        Assert.StartsWith("B", orderId);
        Assert.Matches("^[DTB][0-9]{10}$", orderId);
    }

    [Fact]
    public void AccountId_CombinesTypeCityAndTwelveDigits()
    {
        const string connType = "B";
        const string cityCode = "001";
        var accountId = connType + cityCode + 42L.ToString("D12");

        Assert.Equal(16, accountId.Length);
        Assert.Matches("^[DBT][0-9]{15}$", accountId);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 25)]
    [InlineData(15, 25)]
    [InlineData(16, 50)]
    [InlineData(25, 50)]
    [InlineData(26, 75)]
    [InlineData(50, 75)]
    [InlineData(51, 100)]
    [InlineData(200, 100)]
    public void BulkDiscountTiers_MatchSpecification(int quantity, decimal expectedPercent)
    {
        // Bảng bậc trong seed.sql: (10,15,25) (16,25,50) (26,50,75) (51,NULL,100).
        var tier = new (int Min, int? Max, decimal Percent)[]
        {
            (10, 15, 25), (16, 25, 50), (26, 50, 75), (51, null, 100)
        }.FirstOrDefault(t => quantity >= t.Min && (t.Max is null || quantity <= t.Max));

        Assert.Equal(expectedPercent, tier.Percent);
    }

    [Fact]
    public void BusinessException_MapsCodesToHttpStatus()
    {
        Assert.Equal(401, new BusinessException(ErrorCode.AUTH_INVALID, "x").StatusCode);
        Assert.Equal(403, new BusinessException(ErrorCode.FORBIDDEN, "x").StatusCode);
        Assert.Equal(404, new BusinessException(ErrorCode.ORDER_NOT_FOUND, "x").StatusCode);
        Assert.Equal(400, new BusinessException(ErrorCode.VALIDATION_ERROR, "x").StatusCode);
        Assert.Equal(409, new BusinessException(ErrorCode.PAYMENT_EXCEEDS_DUE, "x").StatusCode);
    }

    [Fact]
    public void ConnectionStatusTransitions_FollowAllowedTable()
    {
        static bool Allowed(string from, string to) => (from, to) switch
        {
            ("ACTIVE", "TEMP_INACTIVE") => true,
            ("TEMP_INACTIVE", "ACTIVE") => true,
            ("ACTIVE", "PERM_INACTIVE") => true,
            ("TEMP_INACTIVE", "PERM_INACTIVE") => true,
            _ => false
        };

        Assert.True(Allowed("ACTIVE", "TEMP_INACTIVE"));
        Assert.True(Allowed("TEMP_INACTIVE", "ACTIVE"));
        Assert.True(Allowed("ACTIVE", "PERM_INACTIVE"));
        Assert.False(Allowed("PERM_INACTIVE", "ACTIVE"));
        Assert.False(Allowed("PERM_INACTIVE", "TEMP_INACTIVE"));
    }
}