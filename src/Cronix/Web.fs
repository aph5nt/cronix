namespace Cronix

open System
open Nancy
open Nancy.Owin
open Logging
open Nancy.Responses
open Nancy.TinyIoc
open Nancy.Bootstrapper
open Microsoft.AspNet.SignalR
open Microsoft.AspNet.SignalR.Hubs
open FSharp.Control.Reactive
open Microsoft.Owin.Builder
open Microsoft.Owin.Hosting

module Web = 
    
    (* SignalR *)
    type IJobHub =
        abstract member UpdateState: JobState -> unit

    [<HubName("Jobs")>]
    type JobHub(scheduleManager : IScheduleManager) as this =
        inherit Hub<IJobHub>()
        let logger = logger()
        let observer = Observable.toObservable scheduleManager.State
        member x.GetInitialState() =
            let result = scheduleManager.State |> Seq.toList
            result
        member x.StartObserving() =
            observer.Subscribe(fun(jobState) -> this.Clients.All.UpdateState(jobState))
     
    (* NancyFx *)
    type TriggerModule(scheduleManager : IScheduleManager) as self = 
        inherit NancyModule()
        do
            self.Get.["/trigger"] <- fun _ -> self.Index()
                
        member self.Index() =
            let result = scheduleManager.State |> Seq.toArray
            let response = new JsonResponse<JobState[]>(result, new DefaultJsonSerializer())
            response.StatusCode <- HttpStatusCode.OK;
            response :> obj
                  
    (* Startup *)
    type TinyIoCDependencyResolver(container : TinyIoCContainer) =
        inherit DefaultDependencyResolver()
        override x.GetService(serviceType : Type) =
             if container.CanResolve(serviceType) = true then container.Resolve(serviceType) else base.GetService(serviceType)
        override x.GetServices(serviceType : Type) =
             let objects = if container.CanResolve(serviceType) = true then container.ResolveAll(serviceType) else Seq.empty<obj>
             let baseObjects = base.GetServices(serviceType)
             let result =  objects |> Seq.append baseObjects
             result

    type WebBootstrapper(scheduleManager : IScheduleManager) =
        inherit DefaultNancyBootstrapper()
        override this.ApplicationStartup(container : TinyIoCContainer, pipelines : IPipelines ) =
            container.Register<IScheduleManager>(scheduleManager) |> ignore
            GlobalHost.DependencyResolver <- new TinyIoCDependencyResolver(container);
            base.ApplicationStartup(container, pipelines)
   
    let initialize(scheduleManager : IScheduleManager) = 
        let startWebApp (app : Owin.IAppBuilder) scheduleManager = 
            let options = new NancyOptions()
            options.Bootstrapper <- new WebBootstrapper(scheduleManager)
            app.Use(NancyMiddleware.UseNancy(options)) |> ignore

            let config = new HubConfiguration(EnableDetailedErrors = true)
            Owin.OwinExtensions.MapSignalR(app, "/signalrHub", config) |> ignore
            ()

        try
            WebApp.Start("http://localhost:8085", fun(app:Owin.IAppBuilder) -> (startWebApp app scheduleManager)) |> ignore
        with
        | exn -> ()

   
            
    
    

    

   

   

  
 