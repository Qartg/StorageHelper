using StorageHelper.Models;
using StorageHelper.Services;

namespace StorageHelper.Tests
{
    public class BuildReviewTests
    {
        private readonly PricingService _sut = new();

        // Хелпер: строит товар с историей цен.
        // prices — пары (цена, дата); остальное с дефолтами, чтобы в тесте писать только нужное.
        private static Item MakeItem(string name, int parLevel, int stock,
            bool active = true, bool orderable = true,
            params (decimal price, DateTime date)[] prices)
        {
            var item = new Item
            {
                Name = name,
                Sku = name,
                ParLevel = parLevel,
                CurrentOnStorage = stock,
                IsActive = active,
                IsOredrable = orderable
            };
            foreach (var (price, date) in prices)
                item.PriceRecords.Add(new PriceRecord { Price = price, CapturedAt = date });
            return item;
        }

        [Fact]
        public void BuildReview_ReorderableItem_LineHasReorderQtyAndTotal()
        {
            // склад 3, цель 10 → дозаказать 7; цена 20 → строка 140
            var items = new[]
            {
                MakeItem("A", parLevel: 10, stock: 3, prices: (20m, new DateTime(2026, 1, 1)))
            };

            var (lines, total) = _sut.BuildReview(items);
            var line = Assert.Single(lines);          // ровно одна строка

            Assert.Equal(7, line.Quantity);           // кол-во к ЗАКАЗУ, не остаток
            Assert.Equal(20m, line.CurrentPrice);
            Assert.Equal(140m, line.LineTotal);
            Assert.Equal(140m, total);
        }

        [Fact]
        public void BuildReview_Inactive_Skipped()
        {
            var items = new[] { MakeItem("A", 10, 0, active: false, prices: (20m, new DateTime(2026, 1, 1))) };
            var (lines, _) = _sut.BuildReview(items);
            Assert.Empty(lines);
        }

        [Fact]
        public void BuildReview_NotOrderable_Skipped()
        {
            var items = new[] { MakeItem("A", 10, 0, orderable: false, prices: (20m, new DateTime(2026, 1, 1))) };
            var (lines, _) = _sut.BuildReview(items);
            Assert.Empty(lines);
        }

        [Fact]
        public void BuildReview_AtOrAbovePar_Skipped()
        {
            var items = new[] { MakeItem("A", 10, 10, prices: (20m, new DateTime(2026, 1, 1))) };
            var (lines, _) = _sut.BuildReview(items);
            Assert.Empty(lines);
        }

        [Fact]
        public void BuildReview_ItemWithoutPrice_ShownButNotInTotal()
        {
            var items = new[]
            {
                MakeItem("A", 10, 3, prices: (20m, new DateTime(2026, 1, 1))),   // строка 7×20=140
                MakeItem("B", 10, 0)                                            // цен нет
            };

            var (lines, total) = _sut.BuildReview(items);

            Assert.Equal(2, lines.Count());
            Assert.Contains(lines, l => l.Name == "B" && l.CurrentPrice == null && l.LineTotal == null);
            Assert.Equal(140m, total);   // B в сумму не попал
        }

        [Fact]
        public void BuildReview_NothingToOrder_EmptyAndZeroTotal()
        {
            var items = new[] { MakeItem("A", 5, 5, prices: (20m, new DateTime(2026, 1, 1))) };
            var (lines, total) = _sut.BuildReview(items);
            Assert.Empty(lines);
            Assert.Equal(0m, total);
        }
    }
}
