﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TradingLib;

namespace TradingPriceUpdater
{
	public partial class PriceUpdaterService : ServiceBase
	{
		private Timer _timer;

		public CurrencyPair currency;
		public ApiReader APR;
		public DatabaseConnector DBC;
		public int LastInsert;

		public PriceUpdaterService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			APR = new ApiReader(); // Initialises ApiReader
			DBC = new DatabaseConnector(); // Initialises DatabaseConnector

			currency = new CurrencyPair(2, 1); // Creates new currency pair of ETH/BTC

			_timer = new Timer(10 * 1000); // Create timer to run every 10 seconds
			_timer.Elapsed += new System.Timers.ElapsedEventHandler(TimerElapsed); // Sets method to be run when the timer is elapsed
			_timer.Start(); // Starts the timer
		}

		protected override void OnStop()
		{
			DBC.CloseConnection(); // Closes the connection
		}

		/// <summary>
		/// Run every 10 seconds, gets data from the API and puts it into the database.
		/// </summary>
		public void TimerElapsed(object sender, ElapsedEventArgs e)
		{
			LastInsert = GetUnixTime();

			TickerResult ticker = APR.GetTickerResult(currency); // Gets the ticker for the specified currency
			TradeHistoryResult history = APR.GetTradeHistoryResult(currency); // Gets the trade history for the specified currency

			double volAsk = 0;
			double volBid = 0;

			foreach (HistoricalTrade trade in history.trades)
			{
				if (trade.IsBid)
					volBid += trade.Amount;
				else
					volAsk += trade.Amount;
			}

			DatabaseRow row = new DatabaseRow(LastInsert, ticker.lastPrice, volBid, volAsk);
			DBC.InsertIntoDatabase(row);
		}

		public int GetUnixTime()
		{
			TimeSpan timeDifference = DateTime.UtcNow -
			                          new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			int unixTime = System.Convert.ToInt32(timeDifference.TotalSeconds);

			return unixTime;
		}
	}
}
