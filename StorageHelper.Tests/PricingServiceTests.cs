using StorageHelper.Models;
using StorageHelper.Services;

namespace StorageHelper.Tests
{
    public class PricingServiceTests
    {
        // sut = "system under test" — то, что проверяем
        private readonly PricingService _sut = new();

        // маленький помощник: короткое создание записи цены
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


        // ---------- ToOrder ----------
        // [Theory] + [InlineData] — один тест, много наборов данных.
        // Параметры (current, parLevel, expected) подставляются из InlineData.
        [Theory]
        [InlineData(5, 10, 5)]    // на складе меньше цели → дозаказать разницу
        [InlineData(0, 8, 8)]     // пусто → дозаказать всю цель
        [InlineData(10, 10, 0)]   // ровно по цели → ничего
        [InlineData(12, 10, 0)]   // больше цели → не уходим в минус
        public void ToOrder_ReturnsNonNegativeDifference(int current, int parLevel, int expected)
        {
            int result = _sut.ToOrder(current, parLevel);
            Assert.Equal(expected, result);
        }

        // ---------- CalculateStats: краевые случаи ----------
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

        [Fact] // [Fact] — обычный тест без параметров
        public void CalculateStats_NoRecords_AllNull()
        {
            // Arrange (подготовка) — пустой список
            // Act (действие)
            var stats = _sut.CalculateStats(new List<PriceRecord>());
            // Assert (проверка)
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
            Assert.Null(stats.Previous);        // сравнивать не с чем
            Assert.Equal(100m, stats.Minimum);  // минимум из одной записи — она сама
        }

        // ---------- CalculateStats: основной случай ----------

        [Fact]
        public void CalculateStats_ManyRecords_ReturnsLatestPreviousAndMin()
        {
            var records = new[]
            {
                Price(100m, new DateTime(2026, 1, 1)),
                Price(90m,  new DateTime(2026, 1, 2)),
                Price(120m, new DateTime(2026, 1, 3)),   // самая свежая
            };

            var stats = _sut.CalculateStats(records);

            Assert.Equal(120m, stats.Current);    // последняя по дате
            Assert.Equal(90m,  stats.Previous);   // предпоследняя по дате
            Assert.Equal(90m,  stats.Minimum);    // минимум из всех
        }

        [Fact]
        public void CalculateStats_UnorderedInput_StillCorrect()
        {
            // те же данные, но порядок перемешан — результат должен совпадать
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
