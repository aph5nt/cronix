namespace Cronix.Web

open System
open Nancy
open Microsoft.Owin.Hosting

module EntryPoint = 
    open Microsoft.AspNet.SignalR

    [<EntryPoint>]
    let main args =
        
        // Owin configuration
        let options = new StartOptions()
        options.Port <- new Nullable<int>(8111)
         
        // Nancy configuration
        let nancyOptions = new Nancy.Owin.NancyOptions()
        nancyOptions.Bootstrapper <- new DefaultNancyBootstrapper()

        // signalr configuration
        let config = new HubConfiguration(EnableDetailedErrors = true)

        WebApp.Start(options,
            fun(app : Owin.IAppBuilder) -> (
                                             app.Use(Nancy.Owin.NancyMiddleware.UseNancy(nancyOptions)) |> ignore
                                             Owin.OwinExtensions.MapSignalR(app, "/signalrHub", config) |> ignore
            )) |> ignore
 
 
        Console.WriteLine("Press any [Enter] to close the host.")
        Console.ReadLine() |> ignore
        0
