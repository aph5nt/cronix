namespace Cronix.Web

open Nancy
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs

type ISampleHub =
   abstract member GetData: string -> unit
   abstract member StartJob: string -> unit
   abstract member StopJob: string -> unit

[<HubName("SampleHub")>]
type SampleHub() =
    inherit Hub<ISampleHub>()

    // observable dictionary ->    base.Clients.All.GetData(name) |> ignore

    member x.GetData(name : string) =
        base.Clients.All.GetData(name) |> ignore

    member x.StartJob(name : string) =
        System.Threading.Thread.Sleep(3000)
        base.Clients.All.GetData(name) |> ignore

    member x.StopJob(name : string) =
        System.Threading.Thread.Sleep(3000)
        base.Clients.All.GetData(name) |> ignore

type IndexModule() as x =
    inherit NancyModule()
    do 
        x.Get.["/"] <- fun _ -> x.Index()

    member x.Index() =
        box base.View.["index"]

