﻿using Azure.Core;
using DotNetCoreSqlDb.Data;
using DotNetCoreSqlDb.Models;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Web;

namespace DotNetCoreSqlDb.Service;

public class ETrade
{

    public Option GetOptionDetails(CoreDbContext context, User user, string optionDate, string symbol, double price, double strikePriceNear, string chainType, long batchId, string batchDateTime)
    {
        DateTime optionDateLocal = DateTime.Parse(optionDate, new CultureInfo("en-US", true));

        int expiryYear = optionDateLocal.Year;
        int expiryMonth = optionDateLocal.Month;
        int expiryDay = optionDateLocal.Day;
        int noOfStrikes = 1;

        List<Option> options = new List<Option>();
        JObject optionJson = new JObject();
        try
        {
            string etradeBaseUrl = user.EtradeBaseUrl;
            var client = new RestClient(etradeBaseUrl);

            symbol = symbol.Equals("SPXW") ? "SPX" : symbol;

            var quoteRequest = new RestRequest($"v1/market/optionchains?symbol={symbol}&expiryYear={expiryYear}&expiryMonth={expiryMonth}&expiryDay={expiryDay}&includeWeekly={false}&chainType={chainType}&strikePriceNear={strikePriceNear}&noOfStrikes={noOfStrikes}");
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(user.ConsumerKey, user.ConsumerSecret, user.AccessToken, user.AccessTokenSecret);
            var response = client.Execute(quoteRequest);

            optionJson = JObject.Parse(response.Content);
            options = BuildOptionBase(batchId, batchDateTime, price, optionJson.ToString());

            context.Option.AddRange(options);
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return options.First();
    }

    public void OpenOrderOptionBuying(CoreDbContext context, Option option, int singalId)
    {
        var orderBookOptionBuying = new OrderBookOptionBuying
        {
            symbol = option.Symbol,
            optionType = option.OptionType,
            optionDate = option.OptionDate,
            strikePrice = option.StrikePrice,
            openBatch = option.Batch,
            openBatchDateTime = option.BatchDateTime,
            openSinalId = singalId,
            openStokePrice = option.Price,
            openCost = option.Ask,
            openDateTime = Help.GetEstDatetime(),
            closeBatchDateTime = option.BatchDateTime
        };

        context.Add(orderBookOptionBuying);
        context.SaveChanges();
    }

    public void CloseOrderOptionBuying(CoreDbContext context, OrderBookOptionBuying orderBookOptionBuying, Option option, int singalId)
    {
        orderBookOptionBuying.closeBatch = option.Batch;
        orderBookOptionBuying.closeBatchDateTime = option.BatchDateTime;
        orderBookOptionBuying.closeDateTime = Help.GetEstDatetime();
        orderBookOptionBuying.closeSinalId = singalId;
        orderBookOptionBuying.closeStokePrice = option.Price;
        orderBookOptionBuying.closeCost = option.Bid;
        orderBookOptionBuying.PnL = Math.Round(orderBookOptionBuying.closeCost - orderBookOptionBuying.openCost, 2);

        context.Update(orderBookOptionBuying);
        context.SaveChanges();
    }

    public void OpenOrderOptionSelling(CoreDbContext context, Option option, int singalId)
    {
        var orderBookOptionSelling = new OrderBookOptionSelling
        {
            symbol = option.Symbol,
            optionType = option.OptionType,
            optionDate = option.OptionDate,
            strikePrice = option.StrikePrice,
            openBatch = option.Batch,
            openBatchDateTime = option.BatchDateTime,
            openSinalId = singalId,
            openStokePrice = option.Price,
            openCost = option.Bid,
            openDateTime = Help.GetEstDatetime(),
            closeBatchDateTime = option.BatchDateTime
        };

        context.Add(orderBookOptionSelling);
        context.SaveChanges();
    }

    public void CloseOrderOptionSelling(CoreDbContext context, OrderBookOptionSelling orderBookOptionSelling, Option option, int singalId)
    {
        orderBookOptionSelling.closeBatch = option.Batch;
        orderBookOptionSelling.closeBatchDateTime = option.BatchDateTime;
        orderBookOptionSelling.closeDateTime = Help.GetEstDatetime();
        orderBookOptionSelling.closeSinalId = singalId;
        orderBookOptionSelling.closeStokePrice = option.Price;
        orderBookOptionSelling.closeCost = option.Ask;
        orderBookOptionSelling.PnL = Math.Round(orderBookOptionSelling.openCost - orderBookOptionSelling.closeCost, 2);

        context.Update(orderBookOptionSelling);
        context.SaveChanges();
    }

    public List<Option> BuildOptionBase(long timeStamp, string batchDateTime, double price, string optionJson)
    {
        var optionChain = JsonConvert.DeserializeObject<OptionChain>(optionJson);

        List<Option> calls = new List<Option>();
        foreach (var item in optionChain.OptionChainResponse.OptionPair)
        {
            if (item.Call != null && item.Call.OptionRootSymbol != "SPX")
            {
                item.Call.Batch = timeStamp;
                item.Call.BatchDateTime = batchDateTime;
                item.Call.Price = price;
                calls.Add((Option)item.Call);
            }

            if (item.Put != null && item.Put.OptionRootSymbol != "SPX")
            {
                item.Put.Batch = timeStamp;
                item.Put.BatchDateTime = batchDateTime;
                item.Put.Price = price;
                calls.Add((Option)item.Put);
            }
        }
        foreach (var item in calls)
        {
            item.Symbol = item.OptionRootSymbol;
            item.Rho = item.Rho;
            item.Vega = item.Vega;
            item.Theta = item.Theta;
            item.Delta = item.Delta;
            item.Gamma = item.Gamma;
            item.Iv = item.Iv;


            item.CurrentValue = item.CurrentValue;

            item.OptionPrice = item.OptionPrice;
            item.OptionDate = item.OptionDate;
            item.MinutesToExpire = item.MinutesToExpire;
        }
        return calls;

    }

    public string HealthCheck(CoreDbContext context, string symbol, DateTimeOffset now)
    {
        List<Option> options = new List<Option>();
        JObject optionJson = new JObject();
        try
        {
            var users = context.User.ToList();
            var user = users.FirstOrDefault();

            string etradeBaseUrl = user.EtradeBaseUrl;
            var client = new RestClient(etradeBaseUrl);

            var quoteRequest = new RestRequest($"v1/market/quote/{symbol}");
            client.Authenticator = OAuth1Authenticator.ForProtectedResource(user.ConsumerKey, user.ConsumerSecret, user.AccessToken, user.AccessTokenSecret);
            var response = client.Execute(quoteRequest);

            if (response.Content != null && response.Content.Contains("oauth_problem"))
                new Exception($"|****** OAUTH PROBLEM ******|");

            var stockQuote = JsonConvert.DeserializeObject<StockQuoteResponse>(response.Content.ToString());

            var price = 0.0;
            if (stockQuote != null && stockQuote.QuoteResponse.QuoteData.Count > 0)
                price = stockQuote.QuoteResponse.QuoteData[0].All.ask;

            return $"{symbol} : {price}";
        }
        catch (Exception ex)
        {
            return "Error:" + ex.Message;
        }
    }

    public void RunOrderBookOptionBuying(CoreDbContext _context, TVSignal tVSignal, string optionDate, int hoursLeft, string emaPeriodFactor)
    {
        DateTimeOffset now = (DateTimeOffset)Help.GetEstDatetime();
        var batchId = now.ToUnixTimeSeconds();
        var batchDateTime = now.ToString("yyyy-MM-dd hh:mm:ss.fff tt");
        double awayFactor = (tVSignal.Price * 0.001) + (hoursLeft * 1.5);
        if (tVSignal.Simbol.ToUpper().Equals("SPY")) awayFactor = ((10 * tVSignal.Price * 0.001) + (hoursLeft * 1.5)) * 0.1;

        ETrade etrade = new ETrade();
        var users = _context.User.ToList();

        if (tVSignal.Signal.ToUpper().Equals("LONG") && tVSignal.Period.Equals(emaPeriodFactor))
        {
            string closeOptionType = "PUT";
            string optionType = "CALL";

            var putOrderBook = _context.OrderBookOptionBuying.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == closeOptionType && a.closeBatch <= 0).ToList();
            foreach (var order in putOrderBook)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, order.strikePrice, order.optionType, batchId, batchDateTime);
                etrade.CloseOrderOptionBuying(_context, order, option, tVSignal.id);
            }
            double strikePriceNear = Math.Round(tVSignal.Price + awayFactor);
            var callOrderBook = _context.OrderBookOptionBuying.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == optionType && a.closeBatch <= 0).ToList();
            if (callOrderBook.Count <= 0)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, Convert.ToInt64(strikePriceNear), optionType, batchId, batchDateTime);
                etrade.OpenOrderOptionBuying(_context, option, tVSignal.id);
            }
        }
        else if (tVSignal.Signal.ToUpper().Equals("SHORT") && tVSignal.Period.Equals(emaPeriodFactor))
        {
            string closeOptionType = "CALL";
            string optionType = "PUT";
            var callOrderBook = _context.OrderBookOptionBuying.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == closeOptionType && a.closeBatch <= 0).ToList();

            foreach (var order in callOrderBook)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, order.strikePrice, order.optionType, batchId, batchDateTime);
                etrade.CloseOrderOptionBuying(_context, order, option, tVSignal.id);
            }
            double strikePriceNear = Math.Round(tVSignal.Price - awayFactor);
            var putOrderBook = _context.OrderBookOptionBuying.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == optionType && a.closeBatch <= 0).ToList();
            if (putOrderBook.Count <= 0)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, Convert.ToInt64(strikePriceNear), optionType, batchId, batchDateTime);
                etrade.OpenOrderOptionBuying(_context, option, tVSignal.id);
            }
        }
    }

    public void RunOrderBookOptionSelling(CoreDbContext _context, TVSignal tVSignal, string optionDate, int hoursLeft, string emaPeriodFactor)
    {
        DateTimeOffset now = (DateTimeOffset)Help.GetEstDatetime();
        var batchId = now.ToUnixTimeSeconds();
        var batchDateTime = now.ToString("yyyy-MM-dd hh:mm:ss.fff tt");
        double awayFactor = (tVSignal.Price * 0.00125) - hoursLeft;
        if (tVSignal.Simbol.ToUpper().Equals("SPY")) awayFactor = ((10 * tVSignal.Price * 0.00125) - hoursLeft) * 0.1;

        ETrade etrade = new ETrade();
        var users = _context.User.ToList();

        if (tVSignal.Signal.ToUpper().Equals("LONG") && tVSignal.Period.Equals(emaPeriodFactor))
        {
            string closeOptionType = "CALL";
            string optionType = "PUT";
            var callOrderBook = _context.OrderBookOptionSelling.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == closeOptionType && a.closeBatch <= 0).ToList();

            foreach (var order in callOrderBook)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, order.strikePrice, order.optionType, batchId, batchDateTime);
                etrade.CloseOrderOptionSelling(_context, order, option, tVSignal.id);
            }

            double strikePriceNear = Math.Round(tVSignal.Price - awayFactor);
            var putOrderBook = _context.OrderBookOptionSelling.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == optionType && a.closeBatch <= 0).ToList();
            if (putOrderBook.Count <= 0)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, Convert.ToInt64(strikePriceNear), optionType, batchId, batchDateTime);
                etrade.OpenOrderOptionSelling(_context, option, tVSignal.id);
            }
        }

        if (tVSignal.Signal.ToUpper().Equals("SHORT") && tVSignal.Period.Equals(emaPeriodFactor))
        {
            string closeOptionType = "PUT";
            string optionType = "CALL";

            var putOrderBook = _context.OrderBookOptionSelling.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == closeOptionType && a.closeBatch <= 0).ToList();
            foreach (var order in putOrderBook)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, order.strikePrice, order.optionType, batchId, batchDateTime);
                etrade.CloseOrderOptionSelling(_context, order, option, tVSignal.id);
            }
            double strikePriceNear = Math.Round(tVSignal.Price + awayFactor);
            var callOrderBook = _context.OrderBookOptionSelling.ToList().Where(a => a.symbol.Contains(tVSignal.Simbol) && Convert.ToDateTime(a.optionDate) == Convert.ToDateTime(optionDate) && a.optionType == optionType && a.closeBatch <= 0).ToList();
            if (callOrderBook.Count <= 0)
            {
                var option = etrade.GetOptionDetails(_context, users[0], optionDate, tVSignal.Simbol, tVSignal.Price, Convert.ToInt64(strikePriceNear), optionType, batchId, batchDateTime);
                etrade.OpenOrderOptionSelling(_context, option, tVSignal.id);
            }
        }
    }

    public void CloseAll(CoreDbContext _context)
    {
        var users = _context.User.ToList();
        DateTimeOffset now = (DateTimeOffset)Help.GetEstDatetime();
        var batchId = now.ToUnixTimeSeconds();
        var batchDateTime = now.ToString("yyyy-MM-dd hh:mm:ss.fff tt");

        var callOrderBookOptionBuying = _context.OrderBookOptionBuying.ToList().Where(a => a.closeBatch <= 0).ToList();
        foreach (var order in callOrderBookOptionBuying)
        {
            var option = this.GetOptionDetails(_context, users.First(), order.optionDate, order.symbol, order.strikePrice, order.strikePrice, order.optionType, batchId, batchDateTime);
            this.CloseOrderOptionBuying(_context, order, option, order.id);
        }

        var callOrderBookOptionSelling = _context.OrderBookOptionSelling.ToList().Where(a => a.closeBatch <= 0).ToList();
        foreach (var order in callOrderBookOptionSelling)
        {
            var option = this.GetOptionDetails(_context, users.First(), order.optionDate, order.symbol, order.strikePrice, order.strikePrice, order.optionType, batchId, batchDateTime);
            this.CloseOrderOptionSelling(_context, order, option, order.id);
        }        
    }
}

