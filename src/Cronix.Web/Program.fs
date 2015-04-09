namespace Cronix.Web


open System
open Nancy
open Microsoft.Owin.Hosting
 
 

module EntryPoint = 




    [<EntryPoint>]
    let main args =
        let uri = Uri("http://localhost:8085")

        // Owin configuration
        let options = new StartOptions()
        options.Port <- new Nullable<int>(8085)
         
        // Nancy configuration
        let nancyOptions = new Nancy.Owin.NancyOptions()
        nancyOptions.Bootstrapper <- new Bootstrapper()

        WebApp.Start(options,
            fun(app : Owin.IAppBuilder) -> (
                                             app.Use(Nancy.Owin.NancyMiddleware.UseNancy(nancyOptions)) |> ignore
            )) |> ignore
 
        Console.WriteLine("Your application is running on " + uri.AbsoluteUri)
        Console.WriteLine("Press any [Enter] to close the host.")
        Console.ReadLine() |> ignore
        0
