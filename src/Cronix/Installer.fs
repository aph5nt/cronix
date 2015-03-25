namespace Cronix

module ProjectInstaller =

    open System.Collections
    open System.Configuration.Install
    open System.Reflection
    open System.ServiceProcess
    open Chessie.ErrorHandling
    open Logging
    open System.IO
    open System

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

        installer.Context <- new InstallContext()
        installer.Context.Parameters.["assemblypath"] <- assembly.Location;


    let Install : Install =
        fun(assembly) ->
            let installer = new Installer()
            initialize installer assembly

            let mutable state = new Hashtable()
            try
                installer.Install(state)
                installer.Commit(state)
                installer.Dispose()
                ok("installed")
            with
            | exn ->
                try 
                    installer.Rollback(state)
                    installer.Dispose()
                    fail("rollback: " + exn.Message)
                with
                | ex -> fail("failed to rollback: " + ex.Message)

    let Uninstall : Uninstall = 
        fun(assembly) ->
            try
                let installer = new Installer()
                initialize installer assembly
                installer.Uninstall(null);
                installer.Dispose()
                ok("uninstalled")
            with 
            | ex -> fail("failed to uninstall: " + ex.Message)


    type ServiceProcessAdapter(service : IScheduleManager, setup) =
        inherit ServiceBase()
        do
            (* Set default directory for windows service *)
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory)

        override x.OnStart(args : string[]) = 
            logger.Debug("starting service")
            service.Start()
            setup()
         
        override x.OnStop() = 
            logger.Debug("stopping service")
            service.Stop()


