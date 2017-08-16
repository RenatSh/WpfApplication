using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WpfApplication
{
    public class DealCaptureModel
    {
        private readonly ConcurrentQueue<Stock> stocks = new ConcurrentQueue<Stock>();
        private readonly IMarketValueCalculator marketValueCalculator = new MarketValueCalculator();
        private readonly ITransactionCostCalculator transactionCostCalculator = new TransactionCostCalculator();
        private readonly IReadOnlyDictionary<StockType, IStockNameFactory> stockNamesFactories;

        public DealCaptureModel(long equityNameSeed, long bondNameSeed)
        {
            this.stockNamesFactories = new Dictionary<StockType, IStockNameFactory>
            {
                { StockType.Equity, new StockNameFactory("Equity", equityNameSeed) },
                { StockType.Bond, new StockNameFactory("Bond", equityNameSeed) }
            };
        }

        public IReadOnlyList<Stock> GetStocks()
        {
            return this.stocks.ToList();
        }

        public void AddEquity(decimal? price, decimal? quantity)
        {
            RequireValidInputForNewStock(price, quantity);

            this.stocks.Enqueue(StockFactory(StockType.Equity, price: price.Value, quantity: quantity.Value));
        }

        public void AddBond(decimal? price, decimal? quantity)
        {
            RequireValidInputForNewStock(price, quantity);

            this.stocks.Enqueue(StockFactory(StockType.Bond, price: price.Value, quantity: quantity.Value));
        }

        private static void RequireValidInputForNewStock(decimal? price, decimal? quantity)
        {
            var errors = new List<string>();

            if (price == null)
                errors.Add("Price is required");
            if (quantity == null)
                errors.Add("Quantity is required");

            if (errors.Any())
                throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        private Stock StockFactory(StockType stockType, decimal price, decimal quantity)
        {
            string name = this.stockNamesFactories[stockType].GenerateName();
            var marketValue = this.marketValueCalculator.CalculateMarketValue(price, quantity);
            var cost = this.transactionCostCalculator.CalculateTransactionCost(stockType, marketValue);
            var result = new Stock(
                stockType, 
                name,
                price: price, 
                quantity: quantity, 
                marketValue: marketValue, 
                transactionCost: cost);
            return result;
        }

        public enum StockType
        {
            Equity = 0,
            Bond
        }

        public class Stock
        {
            public StockType StockType { get; }
            public string StockName { get; }
            public decimal Price { get; }
            public decimal Quantity { get; }
            public decimal MarketValue { get; }
            public decimal TransactionCost { get; }

            public Stock(StockType stockType, string stockName, decimal price, decimal quantity, decimal marketValue, decimal transactionCost)
            {
                this.StockType = stockType;
                this.StockName = stockName;
                this.Price = price;
                this.Quantity = quantity;
                this.MarketValue = marketValue;
                this.TransactionCost = transactionCost;
            }
        }

        public interface IMarketValueCalculator
        {
            decimal CalculateMarketValue(decimal price, decimal quantity);
        }

        private sealed class MarketValueCalculator : IMarketValueCalculator
        {
            public decimal CalculateMarketValue(decimal price, decimal quantity)
            {
                return price * quantity;
            }
        }

        public interface ITransactionCostCalculator
        {
            decimal CalculateTransactionCost(StockType stockType, decimal marketValue);
        }

        private sealed class TransactionCostCalculator : ITransactionCostCalculator
        {
            public decimal CalculateTransactionCost(StockType stockType, decimal marketValue)
            {
                switch (stockType)
                {
                    case StockType.Equity:
                        return 0.005m * Math.Abs(marketValue);
                    case StockType.Bond:
                        return 0.02m * Math.Abs(marketValue);
                    default:
                        throw new ArgumentException("Unsupported stockType: " + stockType);
                }
            }
        }

        public interface IStockNameFactory
        {
            string GenerateName();
        }

        private sealed class StockNameFactory : IStockNameFactory
        {
            private readonly string prefix;
            private long counter;

            public StockNameFactory(string prefix, long seed)
            {
                if (string.IsNullOrEmpty(prefix))throw new ArgumentException(nameof(prefix) + " is null or empty");

                this.prefix = prefix;
                this.counter = seed-1;
            }

            public string GenerateName()
            {
                var nextIndex = Interlocked.Increment(ref this.counter);
                return this.prefix + nextIndex;
            }
        }
    }
}
