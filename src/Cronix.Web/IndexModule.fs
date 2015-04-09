namespace Cronix.Web

open Nancy
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs

type ISampleHub =
   abstract member GetData: string -> string

[<HubName("SampleHub")>]
type SampleHub() =
    inherit Hub<ISampleHub>()
    member x.GetData(input : string) =
        base.Clients.All.GetData(input)

type IndexModule() as x =
    inherit NancyModule()
    do 
        x.Get.["/"] <- fun _ -> x.Index()

    member x.Index() =
        box base.View.["index"]
