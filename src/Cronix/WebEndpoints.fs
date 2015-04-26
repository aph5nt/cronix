namespace Cronix

open Logging
open System
open Owin
open Microsoft.AspNet.SignalR.Hubs
open Microsoft.AspNet.SignalR
open Microsoft.Owin.Hosting
open Microsoft.Owin.Cors
open Nancy

module Hubs = 

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

        member self.TerminateTrigger(name : TriggerName) =
            scheduleManager.TerminateTriggerExecution(name) |> ignore

module Website =
   
    open Nancy.Conventions
    open Nancy.TinyIoc
    open Nancy.Bootstrapper
 
     type WebBootstrapper() =
        inherit DefaultNancyBootstrapper()
        
        override x.ApplicationStartup( container : TinyIoCContainer,  pipelines : IPipelines) =
            let impl viewName model content = 
                String.Concat("webui/", viewName)
            let convestion() = 
                Func<string, obj, ViewEngines.ViewLocationContext, string>(impl)
            base.Conventions.ViewLocationConventions.Add( convestion() )
             
        override x.ConfigureConventions(conventions : NancyConventions) =
             conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", "webui"))
             base.ConfigureConventions(conventions)
    
        type WebUiModule() as self = 
            inherit NancyModule()
            do
                self.Get.["/"] <- fun _ -> self.Index()

            member self.Index() =
                base.View.["index.html"] :> obj

module WebHost = 

    let logger = logger()

    let run(scheduleManager : IScheduleManager) =
        try
            let options = new StartOptions()
            options.Port <- new Nullable<int>(AppSettings.port 8111)
            let config = new HubConfiguration(EnableDetailedErrors = true)
       
            WebApp.Start(options,
               fun(app : Owin.IAppBuilder) -> (
                                               GlobalHost.DependencyResolver.Register(typeof<IScheduleManager>, fun() -> scheduleManager :> obj)
                                               app.UseCors(CorsOptions.AllowAll) |> ignore
                                               Owin.OwinExtensions.MapSignalR(app, "/signalr", config) |> ignore
                                               app.UseNancy() |> ignore
               )) |> ignore
        with
        | exn ->  logger.Error(exn)




