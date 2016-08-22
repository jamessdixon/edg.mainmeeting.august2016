namespace ChickenSoftware.StockAnalyzer

open Accord
open System.Web
open FSharp.Data
open System.Net.Http
open Accord.Statistics
open Accord.Statistics.Models.Regression.Linear

type YahooContext = CsvProvider<"http://ichart.finance.yahoo.com/table.csv?s=MSFT">
type LuisContext = JsonProvider<"../data/Luis.json">

type StockProvider() = 
    member this.GetMostRecentClose(ticker) =
        try
            let stockInfo = YahooContext.Load("http://ichart.finance.yahoo.com/table.csv?s=" + ticker)
            let mostRecent = stockInfo.Rows |> Seq.head
            (float)mostRecent.``Adj Close``
        with
        | :?  System.Net.WebException -> -1.0
            
    member this.PredictStockPrice(ticker, nextDate:System.DateTime) =
        try
            let stockInfo = YahooContext.Load("http://ichart.finance.yahoo.com/table.csv?s=" + ticker)
            let rows = stockInfo.Rows |> Seq.take(20)
            let x = 
                rows
                |> Seq.map(fun r -> r.Date.ToOADate()) 
                |> Seq.toArray
            let y = 
                rows 
                |> Seq.map(fun r -> (float)r.``Adj Close``) 
                |> Seq.toArray

            let regression = SimpleLinearRegression()
            regression.Regress(x,y) |> ignore
            let nextDate' = [|nextDate.ToOADate()|]
            regression.Compute(nextDate') |> Seq.head
        with
        | :?  System.Net.WebException -> -1.0

    member this.GetTicker(phrase:string) =
        try
            use client = new HttpClient()
            let appId = "4015e314-326d-4443-a3ad-854f37352e3d"
            let subscriptionKey = "847c177472014d77924e14b82600f35e"
            let queryString = System.Web.HttpUtility.UrlEncode(phrase)
            let uri = "https://api.projectoxford.ai/luis/v1/application?id=" + appId + "&subscription-key=" + subscriptionKey + "&q=" + queryString
            use message = client.GetAsync(uri).Result
            let response = message.Content.ReadAsStringAsync().Result
            let luis = LuisContext.Parse(response)
            if((luis.Entities |> Seq.length) > 0) then
                let entity = luis.Entities |> Seq.head
                if((float)entity.Score > 0.50) then entity.Entity
                else System.String.Empty
            else
                System.String.Empty
        with
        | :?  System.Net.WebException -> System.String.Empty

        
