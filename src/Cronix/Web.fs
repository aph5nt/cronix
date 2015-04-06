namespace Cronix

open System
open Nancy
open Nancy.Hosting.Self
 
module Web = 

    open Nancy.Responses
    open Nancy.TinyIoc
    open Nancy.Bootstrapper

    type WebBootstrapper(scheduleManager : IScheduleManager) =
        inherit DefaultNancyBootstrapper()
        override this.ApplicationStartup(container : TinyIoCContainer, pipelines : IPipelines ) =
            container.Register<IScheduleManager>(scheduleManager) |> ignore
            
    let initialize(scheduleManager : IScheduleManager) =
        let config = new HostConfiguration()
        //config.UrlReservations <- true
        let bootstrapper = new WebBootstrapper(scheduleManager)
        let host = new NancyHost(new Uri("http://localhost:8085"), bootstrapper, config)
        host.Start()

    type TriggerModule(scheduleManager : IScheduleManager) as self = 
        inherit NancyModule()
        do
            self.Get.["/trigger"] <- fun _ -> self.Index()
                
        member self.Index() =
            let result = scheduleManager.State |> Seq.toArray
            let response = new JsonResponse<JobState[]>(result, new DefaultJsonSerializer())
            response.StatusCode <- HttpStatusCode.OK;
            response :> obj
            