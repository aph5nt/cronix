namespace Cronix.Web

open Nancy
open Microsoft.AspNet.SignalR

type SampleHub() =
    inherit Hub()

type IndexModule() as x =
    inherit NancyModule()
    do 
        x.Get.["/"] <- fun _ -> x.Index()

    member x.Index() =
        box base.View.["index"]
