module Types

// Fiat currencies
[<Measure>] type USD

// Crypto currencies
[<Measure>] type BTC

// Currency pairs
[<Measure>] type BTCUSD = BTC/USD
[<Measure>] type USDBTC = USD/BTC