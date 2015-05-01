namespace Cronix.Web

module Hubs = 
    open Microsoft.AspNet.SignalR.Hubs
    open Microsoft.AspNet.SignalR
    open Cronix

    [<HubName("ScheduleManager")>]
    type ScheduleManagerHub() as self =
        inherit Hub<IScheduleManagerHub>()

        // TODO: Service locator antipattern, but it works just for now...
        let scheduleManager = GlobalHost.DependencyResolver.Resolve(typeof<IScheduleManager>) :?> IScheduleManager
        let state() = scheduleManager.TriggerDetails |> Seq.toArray

        do
            scheduleManager.OnTriggerStateChanged.AddHandler(fun sender args -> self.onStateChangedHandler args)

        member private self.onStateChangedHandler args =
            base.Clients.All.OnStateChanged(args)

        member self.GetData() =
            base.Clients.All.GetData(state()) |> ignore

        member self.EnableTrigger(name : TriggerName) =
            scheduleManager.EnableTrigger(name) |> ignore

        member self.FireTrigger(name : TriggerName) =
                scheduleManager.FireTrigger(name) |> ignore

        member self.DisableTrigger(name : TriggerName) =
            scheduleManager.DisableTrigger(name) |> ignore

        member self.TerminateTrigger(name : TriggerName) = // cancel trigger / stop execution?
            scheduleManager.TerminateTriggerExecution(name) |> ignore

