﻿using System.Linq;
using Serilog;
using MongoDB.Driver;

namespace SimStockMarket.Market.Handlers
{
    public class BidHandler : TradeOfferHandler<Bid>
    {
        private static ILogger Log = Serilog.Log.ForContext<BidHandler>();

        private readonly TradeRequestHandler _tradeHandler;

        public BidHandler(
                IMessageBus bus,
                IMongoCollection<TradeOffer> offers,
                TradeRequestHandler tradeHandler
            ) : base(bus, offers, tradeHandler)
        {
            _tradeHandler = tradeHandler;
        }

        public override void Handle(Bid bid)
        {
            Log.Verbose("Processing {@bid}...", bid);

            var ask = FindSeller(bid);

            if (ask == null || bid.TraderId == ask.TraderId)
            {
                Log.Debug("No seller for {symbol} @ {price} - bid submitted.", bid.Symbol, bid.Price);
                SubmitOffer(bid);
            }
            else
            {
                Log.Debug("Found seller for {symbol} @ {price} - executing trade", ask.Symbol, ask.Price);
                _tradeHandler.Handle(ask, bid);
            }
        }

        internal Ask FindSeller(Bid bid)
        {
            Log.Verbose("Finding seller for {@bid}...", bid);

            return GetOffersBySymbol(bid?.Symbol)
                .OfType<Ask>()
                .OrderByDescending(x => x.Price)
                .ThenBy(x => x.Timestamp)
                .FirstOrDefault(x => x.Price <= bid?.Price);
        }
    }
}
