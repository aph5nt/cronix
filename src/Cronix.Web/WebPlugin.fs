namespace Cronix.Web

open Logging
open System
open Owin
open Microsoft.AspNet.SignalR
open Microsoft.Owin.Hosting
open Microsoft.Owin.Cors

module WebPlugin = 
    open Cronix

    let logger = logger()

    let InitPlugin : InitPlugin =
        fun(scheduleManager) ->
            try
                let options = new StartOptions()
                options.Port <- new Nullable<int>(AppSettings.port 8111)
                let config = new HubConfiguration(EnableDetailedErrors = true)
                config.EnableJSONP <- true    

                WebApp.Start(options,
                    fun(app : Owin.IAppBuilder) -> (
                                                    GlobalHost.DependencyResolver.Register(typeof<IScheduleManager>, fun() -> scheduleManager :> obj)
                                                    app.UseCors(CorsOptions.AllowAll) |> ignore
                                                    Owin.OwinExtensions.MapSignalR(app, "/signalr", config) |> ignore
                                                    app.UseNancy() |> ignore
                    )) |> ignore
            with
            | exn ->  logger.Error(exn)
        

    
       




