using StorageHelper.Models;
using StorageHelper.Services;

namespace StorageHelper.Tests
{
    public class PricingServiceTests
    {
        private readonly PricingService _sut = new();

        private static PriceRecord Price(decimal price, DateTime date)
            => new() { Price = price, CapturedAt = date };

        public static TheoryData<decimal?, decimal?, decimal?> IncreaseVsPreviousData =>
        new()
        {
            { 150m, 120m, 0.25m },
            { 140m, 140m, 0m },
            { 0m, 0m, null },
            { 1m, 1m, 0m },
            { 150m, null, null }
        };

        [Theory]
        [MemberData(nameof(IncreaseVsPreviousData))]
        public void IncreaseVsPrevious_Normal(decimal? current, decimal? previous, decimal? expected)
        {
            decimal? res = _sut.IncreaseVsPrevious(current, previous);
            Assert.Equal(expected, res);
        }


        [Theory]
        [InlineData(5, 10, 5)]   
        [InlineData(0, 8, 8)]    
        [InlineData(10, 10, 0)]  
        [InlineData(12, 10, 0)]  
        public void ToOrder_ReturnsNonNegativeDifference(int current, int parLevel, int expected)
        {
            int result = _sut.ToOrder(current, parLevel);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IncreaseVsMinimum_NormalTest()
        {
            decimal current = 150m;
            decimal min = 56m;

            var res = _sut.IncreaseVsMinimum(current, min);

            Assert.Equal(94, res);
        }

        [Fact]
        public void IncreaseVsMinimum_Zero()
        {
            decimal current = 150m;
            decimal min = 150m;

            var res = _sut.IncreaseVsMinimum(current, min);

            Assert.Equal(0, res);
        }

        [Fact] 
        public void CalculateStats_NoRecords_AllNull()
        {
            var stats = _sut.CalculateStats(new List<PriceRecord>());
            Assert.Null(stats.Current);
            Assert.Null(stats.Previous);
            Assert.Null(stats.Minimum);
        }

        [Fact]
        public void CalculateStats_SingleRecord_CurrentAndMinSet_PreviousNull()
        {
            var records = new[] { Price(100m, new DateTime(2026, 1, 1)) };

            var stats = _sut.CalculateStats(records);

            Assert.Equal(100m, stats.Current);
            Assert.Null(stats.Previous);       
            Assert.Equal(100m, stats.Minimum); 
        }

        [Fact]
        public void CalculateStats_ManyRecords_ReturnsLatestPreviousAndMin()
        {
            var records = new[]
            {
                Price(100m, new DateTime(2026, 1, 1)),
                Price(90m,  new DateTime(2026, 1, 2)),
                Price(120m, new DateTime(2026, 1, 3)),
            };

            var stats = _sut.CalculateStats(records);

            Assert.Equal(120m, stats.Current);  
            Assert.Equal(90m,  stats.Previous); 
            Assert.Equal(90m,  stats.Minimum);  
        }

        [Fact]
        public void CalculateStats_UnorderedInput_StillCorrect()
        {
            var records = new[]
            {
                Price(120m, new DateTime(2026, 1, 3)),
                Price(100m, new DateTime(2026, 1, 1)),
                Price(90m,  new DateTime(2026, 1, 2)),
            };

            var stats = _sut.CalculateStats(records);

            Assert.Equal(120m, stats.Current);
            Assert.Equal(90m,  stats.Previous);
            Assert.Equal(90m,  stats.Minimum);
        }
    }
}
