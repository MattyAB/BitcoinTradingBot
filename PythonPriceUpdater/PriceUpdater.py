"""Script to gather market data from bitfinex Spot Price API."""
import requests
from pytz import utc
from datetime import datetime
import time
from apscheduler.schedulers.blocking import BlockingScheduler
import sqlite3

conn = sqlite3.connect('/home/mattyab/bitcoin-price-prediction/priceData.db')

def tick():
    c = conn.cursor()
    
    ticker = requests.get('https://api.bitfinex.com/v2/ticker/tETHBTC').json()
    depth = requests.get('https://api.bitfinex.com/v2/trades/tETHBTC/hist').json()
    date = time.time()
    price = float(ticker[6])

    asks = []
    bids = []

    for trade in depth:
        if trade[2] < 0:
            asks.append(-trade[2])
        else:
            bids.append(trade[2])

    v_bid = sum([bid for bid in bids])
    v_ask = sum([ask for ask in asks])

    command = 'INSERT INTO prices VALUES (' + str(date) + ', ' + str(price) + ', ' + str(v_bid) + ', ' + str(v_ask) + ');'
    c.execute(command)
    #print(date, price, v_bid, v_ask)

    print('Inserted price: ' + str(price) + ' at time: ' + str(date))

    # Save (commit) the changes
    conn.commit()

def main():
    """Run tick() at the interval of every ten seconds."""
    scheduler = BlockingScheduler(timezone=utc)
    scheduler.add_job(tick, 'interval', seconds=10)
    try:
        scheduler.start()
    except (KeyboardInterrupt, SystemExit):
        pass


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        conn.close()
        print 'Interrupted'
        sys.exit(0)