using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApplication
{
    public class DealCaptureViewModel : ObservableAbstract, INotifyPropertyChanged
    {
        private readonly DealCaptureModel model;
        private readonly IReadOnlyDictionary<DealCaptureModel.StockType, decimal> tolerances;

        internal DealCaptureViewModel(DealCaptureModel model, decimal bondTolerance, decimal equityTolerance)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            this.model = model;
            this.tolerances = new Dictionary<DealCaptureModel.StockType, decimal>
            {
                { DealCaptureModel.StockType.Bond, bondTolerance},
                { DealCaptureModel.StockType.Equity, equityTolerance}
            };

            var createEquityCommand = new ActionToCommandAdapter(() => this.CreateEquity());
            var createBondCommand = new ActionToCommandAdapter(() => this.CreateBond());

            this.NewEquity = new NewStockViewModel(createEquityCommand);
            this.NewEquity.PropertyChanged += (s, e) => this.OnPropertyChanged(nameof(NewEquity));
            this.NewBond = new NewStockViewModel(createBondCommand);
            this.NewBond.PropertyChanged += (s, e) => this.OnPropertyChanged(nameof(NewBond));
        }


        public NewStockViewModel NewEquity { get; }
        public NewStockViewModel NewBond { get; }

        //public ICommand CreateEquityCommand { get; }
        //public ICommand CreateBondCommand { get; }

        private void CreateEquity()
        {
            try
            {
                this.model.AddEquity(price: this.NewEquity.Price, quantity: this.NewEquity.Quantity);
                this.NewEquity.Reset();
                this.UpdateStocksAndTotals();
            }
            catch (Exception e)
            {
                this.NewEquity.ErrorMessage = e.Message;
            }
        }

        private void CreateBond()
        {
            try
            {
                this.model.AddBond(price: this.NewBond.Price, quantity: this.NewBond.Quantity);
                this.NewBond.Reset();
                this.UpdateStocksAndTotals();
            }
            catch (Exception e)
            {
                this.NewBond.ErrorMessage = e.Message;
            }
        }

        private IReadOnlyList<WeightedStockViewModel> stocksViewModel = new List<WeightedStockViewModel>();
        public IReadOnlyList<WeightedStockViewModel> Stocks
        {
            get
            {
                return this.stocksViewModel;
            }
            private set
            {
                this.stocksViewModel = value;
                this.OnPropertyChanged();
            }
        }

        private IReadOnlyList<FundSummaryItemViewModel> fundSummary = new List<FundSummaryItemViewModel>();
        public IReadOnlyList<FundSummaryItemViewModel> FundSummary
        {
            get
            {
                return this.fundSummary;
            }
            private set
            {
                this.fundSummary = value;
                this.OnPropertyChanged();
            }
        }

        private void UpdateStocksAndTotals()
        {
            var stocks = this.model.GetStocks();
            this.Stocks = FillStocksViewModel(stocks, this.tolerances)
                .ToList();
            this.FundSummary = FillFundSummary(stocks, this.Stocks);
        }

        private static IEnumerable<WeightedStockViewModel> FillStocksViewModel(IReadOnlyList<DealCaptureModel.Stock> stocks, IReadOnlyDictionary<DealCaptureModel.StockType, decimal> tolerances)
        {
            if (stocks == null) throw new ArgumentNullException(nameof(stocks));
            if (tolerances == null) throw new ArgumentNullException(nameof(tolerances));

            if (stocks.Count == 1)
            {
                var stock = stocks.Single();
                yield return new WeightedStockViewModel(
                    stock,
                    100m,
                    tolerance: tolerances[stock.StockType],
                    priceScale: DecimalDigits(stock.Price),
                    quantityScale: DecimalDigits(stock.Quantity),
                    marketValueScale: DecimalDigits(stock.MarketValue),
                    transactionCostScale: DecimalDigits(stock.TransactionCost));
            }
            else if (stocks.Count > 1)
            {
                decimal totalMarketValue = stocks.Sum(stock => Math.Abs(stock.MarketValue));
                var priceScale = stocks.Select(stock => DecimalDigits(stock.Price)).Max();
                var quantityScale = stocks.Select(stock => DecimalDigits(stock.Quantity)).Max();
                var marketValueScale = stocks.Select(stock => DecimalDigits(stock.MarketValue)).Max();
                var transactionCostScale = stocks.Select(stock => DecimalDigits(stock.TransactionCost)).Max();

                decimal accumulatedWeight = 0m;
                foreach (var stock in stocks.Take(stocks.Count - 1))
                {
                    var weight = Math.Round(CalculateWeight(stock.MarketValue, totalMarketValue), 4);
                    accumulatedWeight += weight;

                    yield return new WeightedStockViewModel(stock, weight, tolerances[stock.StockType], priceScale, quantityScale, marketValueScale, transactionCostScale);
                }

                yield return new WeightedStockViewModel(
                    stocks.Last(),
                    100m - accumulatedWeight,
                    tolerance: tolerances[stocks.Last().StockType],
                    priceScale: priceScale,
                    quantityScale: quantityScale,
                    marketValueScale: marketValueScale,
                    transactionCostScale: transactionCostScale);
            }
        }

        private static List<FundSummaryItemViewModel> FillFundSummary(IReadOnlyList<DealCaptureModel.Stock> stocks, IReadOnlyList<WeightedStockViewModel> stocksViewModel)
        {
            if (stocks == null) throw new ArgumentNullException(nameof(stocks));
            if (stocksViewModel == null) throw new ArgumentNullException(nameof(stocksViewModel));

            var equityStockWeight = stocksViewModel.Where(stock => stock.StockType == DealCaptureModel.StockType.Equity)
                .Sum(stock => stock.NumericWeight);
            var equityMarketValue = stocks.Where(stock => stock.StockType == DealCaptureModel.StockType.Equity)
                .Sum(stock => stock.MarketValue);

            var bondStockWeight = stocksViewModel.Where(stock => stock.StockType == DealCaptureModel.StockType.Bond)
                .Sum(stock => stock.NumericWeight);
            var bondMarketValue = stocks.Where(stock => stock.StockType == DealCaptureModel.StockType.Bond)
                .Sum(stock => stock.MarketValue);

            int stockWeightScale = Math.Max(
                DecimalDigits(equityStockWeight),
                DecimalDigits(bondStockWeight));
            int marketValueScale = Math.Max(
                DecimalDigits(equityMarketValue),
                DecimalDigits(bondMarketValue));
            
            var result = new List<FundSummaryItemViewModel>
            {
                new FundSummaryItemViewModel(
                    DealCaptureModel.StockType.Equity.ToString(),
                    stocks.Count(stock => stock.StockType == DealCaptureModel.StockType.Equity),
                    totalStockWeight: equityStockWeight,
                    stockWeightScale: stockWeightScale,
                    totalMarketValue: equityMarketValue,
                    marketValueScale: marketValueScale),

                new FundSummaryItemViewModel(
                    DealCaptureModel.StockType.Bond.ToString(),
                    stocks.Count(stock => stock.StockType == DealCaptureModel.StockType.Bond),
                    totalStockWeight: bondStockWeight,
                    stockWeightScale: stockWeightScale,
                    totalMarketValue: bondMarketValue,
                    marketValueScale: marketValueScale),

                new FundSummaryItemViewModel(
                    "All",
                    stocks.Count,
                    totalStockWeight: equityStockWeight + bondStockWeight,
                    stockWeightScale: stockWeightScale,
                    totalMarketValue: equityMarketValue + bondMarketValue,
                    marketValueScale: marketValueScale)
            };

            return result;
        }

        private static decimal CalculateWeight(decimal stockMarketValue, decimal total)
        {
            if (total == 0.0m)
            {
                return 100.0m;
            }
            else
            {
                return (Math.Abs(stockMarketValue) / total) * 100.0m;
            }
        }

        private static int DecimalDigits(decimal value)
        {
            var result = ((System.Data.SqlTypes.SqlDecimal)value).Scale;
            return result;
        }
        private static string FormatDecimalPadRight(decimal value, int decimals)
        {
            if (decimals <= 0)
            {
                return value.ToString("N0");
            }
            else
            {
                if (value < 0)
                {
                    return "-" + FormatDecimalPadRight(Math.Abs(value), decimals);
                }
                else
                {
                    var integer = Math.Floor(value);
                    var fraction = value - integer;

                    if (fraction == 0m)
                    {
                        return integer.ToString("N0") + new string(' ', decimals + 1);
                    }
                    else
                    {
                        return integer.ToString("N0") + string.Format("{0:.###################}", fraction).PadRight(decimals + 1, ' ');
                    }
                }
            }
        }

        public class NewStockViewModel : ObservableAbstract
        {
            public NewStockViewModel(ICommand createCommand)
            {
                if (createCommand == null) throw new ArgumentNullException(nameof(createCommand));

                this.CreateCommand = createCommand;
            }

            public ICommand CreateCommand { get; }


            private decimal? price;
            public decimal? Price
            {
                get
                {
                    return this.price;
                }
                set
                {
                    if (this.price != value)
                    {
                        this.price = value;
                        this.OnPropertyChanged();
                    }
                }
            }

            private decimal? quantity;
            public decimal? Quantity
            {
                get
                {
                    return this.quantity;
                }
                set
                {
                    if (this.quantity != value)
                    {
                        this.quantity = value;
                        this.OnPropertyChanged();
                    }
                }
            }

            private string errorMessage;
            public string ErrorMessage
            {
                get
                {
                    return this.errorMessage;
                }
                set
                {
                    if (this.errorMessage != value)
                    {
                        this.errorMessage = value;
                        this.OnPropertyChanged();
                    }
                }
            }

            public void Reset()
            {
                this.ErrorMessage = null;
                this.Price = null;
                this.Quantity = null;
            }
        }

        public class WeightedStockViewModel
        {
            public DealCaptureModel.StockType StockType { get; }
            public string StockName { get; }
            public string Price { get; }
            public string Quantity { get; }
            public string MarketValue { get; }
            public string TransactionCost { get; }
            public string Weight { get; }
            public bool HighAwareness { get; }

            public decimal NumericWeight { get; }


            public WeightedStockViewModel(DealCaptureModel.Stock stock, decimal weight, decimal tolerance, int priceScale, int quantityScale, int marketValueScale, int transactionCostScale)
            {
                if (stock == null) throw new ArgumentNullException(nameof(stock));

                this.StockName = stock.StockName;
                this.StockType = stock.StockType;
                this.Price = FormatDecimalPadRight(stock.Price, priceScale);
                this.Quantity = FormatDecimalPadRight(stock.Quantity, quantityScale);
                this.MarketValue = FormatDecimalPadRight(stock.MarketValue, marketValueScale);
                this.TransactionCost = FormatDecimalPadRight(stock.TransactionCost, transactionCostScale);

                this.NumericWeight = weight;
                this.Weight = string.Format("{0,7:#0.0000}", weight);

                this.HighAwareness = stock.MarketValue < 0m
                                     || stock.TransactionCost > tolerance;
            }
        }

        public class FundSummaryItemViewModel
        {
            public string SummaryKind { get; }
            public long TotalNumber { get; }
            public string TotalStockWeight { get; }
            public string TotalMarketValue { get; }

            public FundSummaryItemViewModel(string summaryKind, long totalNumber, decimal totalStockWeight, int stockWeightScale, decimal totalMarketValue, int marketValueScale)
            {
                this.SummaryKind = summaryKind;
                this.TotalNumber = totalNumber;
                this.TotalStockWeight = FormatDecimalPadRight(totalStockWeight, stockWeightScale);
                this.TotalMarketValue = FormatDecimalPadRight(totalMarketValue, marketValueScale);
            }
        }
    }
}
