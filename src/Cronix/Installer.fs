namespace Cronix

module ProjectInstaller =

    open System
    open System.Collections
    open System.Configuration.Install
    open System.Reflection
    open System.ServiceProcess
    open Logging

    let logger = logger()

    let initialize (installer : Installer) (assembly : Assembly) =
        let serviceName = assembly.GetName().Name
        let serviceProcessInstaller = new ServiceProcessInstaller()
        serviceProcessInstaller.Account <- ServiceAccount.LocalService
        installer.Installers.Add(serviceProcessInstaller) |> ignore

        let serviceInstaller = new ServiceInstaller()
        serviceInstaller.ServiceName <- serviceName
        serviceInstaller.DisplayName <- serviceName
        serviceInstaller.Description <- "Executes scheduled jobs."
        serviceInstaller.StartType <- ServiceStartMode.Automatic
        installer.Installers.Add(serviceInstaller) |> ignore

        installer.Context = new InstallContext() |> ignore
        installer.Context.Parameters.["assemblypath"] <- assembly.Location;


    let install (assembly : Assembly) =
        let installer = new Installer()
        initialize installer assembly

        let mutable state = new Hashtable()
        try
            installer.Install(state)
            installer.Commit(state)
        with
        | exn ->
            try 
                installer.Rollback(state)
            with
            | _ -> ()

            failwith exn.Message

        installer.Dispose() |> ignore


    let uninstall (assembly : Assembly)  = 
        let installer = new Installer()
        initialize installer assembly
        installer.Uninstall(null);


    type ServiceProcessAdapter(service : IScheduleManager) =
        inherit ServiceBase()
        override x.OnStart(args : string[]) = ()
        override x.OnStop() = service.Stop()
        override x.OnPause() = ()
        override x.OnContinue() = ()