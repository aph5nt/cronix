namespace Cronix.Web

open System
open Nancy
 
open Microsoft.Owin.Hosting
open Microsoft.Owin.Diagnostics
 

module EntryPoint = 
    open Microsoft.AspNet.SignalR

    [<EntryPoint>]
    let main args =
        
        // Owin configuration
        let options = new StartOptions()
        options.Port <- new Nullable<int>(8111)
         
        // Nancy configuration
        let nancyOptions = new Nancy.Owin.NancyOptions()
        nancyOptions.Bootstrapper <- new Bootstrapper()
        
        // signalr configuration
        let config = new HubConfiguration(EnableDetailedErrors = true)
        
        WebApp.Start(options,
            fun(app : Owin.IAppBuilder) -> (
                                           Owin.OwinExtensions.MapSignalR(app, "/signalr", config) |> ignore
                                           app.Use(Nancy.Owin.NancyMiddleware.UseNancy(nancyOptions)) |> ignore
                                           
            )) |> ignore
 
 
        Console.WriteLine("Press any [Enter] to close the host.")
        Console.ReadLine() |> ignore
        0
