namespace BitstampTicker

open FSharp.Data
open FSharp.Data.Sql

module Exchange =

    [<Literal>]
    let connectionString = @"Data Source=.;Initial Catalog=Bitstamp;Integrated Security=True;Pooling=False"

    [<Literal>]
    let fetchUrl = "https://www.bitstamp.net/api/ticker/"

    [<Literal>]
    let sample = """ {"high": "237.99", "last": "236.95", "timestamp": "1431795831", "bid": "236.85", "vwap": "236.57", "volume": "3291.57347664", "low": "235.01", "ask": "236.95"} """

    type Database = SqlDataProvider<ConnectionString = connectionString, DatabaseVendor = Common.DatabaseProviderTypes.MSSQLSERVER, UseOptionTypes = true>
    type BitStamp = JsonProvider<sample>
      
    let insertData() =
        let fetchedData = BitStamp.Parse <| Http.RequestString fetchUrl
        let context = Database.GetDataContext()
        let tick = context.``[dbo].[Ticks]``.Create()
        tick.Ask <- fetchedData.Ask
        tick.Bid <- fetchedData.Bid
        tick.High <- fetchedData.High
        tick.Last <- fetchedData.Last
        tick.Low <- fetchedData.Low
        tick.Timestamp <- fetchedData.Timestamp
        tick.Volume <- fetchedData.Volume
        tick.Vwap <- fetchedData.Vwap
        
        context.SubmitUpdates() |> ignore  
         
