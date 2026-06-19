using StorageHelper.Models;

namespace StorageHelper.Services
{

    public interface IPricingService
    {
        PriceStats CalculateStats(IEnumerable<PriceRecord> records);
        int ToOrder(int current, int parLevel);
        decimal? IncreaseVsPrevious(decimal? current, decimal? previous);
        decimal IncreaseVsMinimum(decimal current, decimal min);
        decimal BudgetImpact(int reorder, decimal current);
    }

    //TODO: расчет роста цены
    public class PricingService : IPricingService
    {
        public PriceStats CalculateStats(IEnumerable<PriceRecord> records)
        {
            PriceRecord? most = null;
            PriceRecord? second = null;
            decimal? min = null;

            foreach (var record in records)
            {
                if(min == null || record.Price < min)
                    min = record.Price;

                if(most == null || record.CapturedAt > most.CapturedAt)
                {
                    second = most;
                    most = record;
                }
                else if(second == null || record.CapturedAt > second.CapturedAt)
                    second = record;
            }

            return new PriceStats()
            {
                Current = most?.Price,
                Previous = second?.Price,
                Minimum = min ?? most?.Price,
            };
        }

        public int ToOrder(int current, int parLevel) => Math.Max(0, parLevel - current);
        public decimal? IncreaseVsPrevious(decimal? current, decimal? previous)
        {
            if (current == null || previous == null|| previous == 0 || current == 0) return null;
            return (current - previous) / previous;
        }
        public decimal IncreaseVsMinimum(decimal current, decimal min) => current - min;
        public decimal BudgetImpact(int reorder, decimal current) => reorder * current;
    }
}
