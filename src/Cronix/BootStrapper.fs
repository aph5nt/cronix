namespace Cronix

module BootStrapper =
   
    open Chessie.ErrorHandling
    open Logging
    open System.IO
    open System
    open System.Reflection
    open System.CodeDom.Compiler
    open FSharp.Compiler.CodeDom
    open System.ServiceProcess
    open System.Diagnostics
 
    let startupFile = "Startup.fsx"
    let outputAssembly = "Cronix.Startup.dll"
    let logger = logger()

    let loadAssemblies() =
        let assemblies =
            Directory.GetFiles(".", "*.dll", SearchOption.TopDirectoryOnly)
            |> Seq.map(fun(f) -> Path.GetFileName(f)) 
            |> Seq.filter ((<>)outputAssembly)
            |> Seq.sort
            |> Seq.toArray 
        let defaultAssemblies  = 
            let entry = Assembly.GetEntryAssembly()
            let executing = Assembly.GetExecutingAssembly()
            let calling = Assembly.GetCallingAssembly()
            [|"System.dll"; 
                (if entry <> null then Path.GetFileName(entry.Location) else "");
                (if executing <> null then Path.GetFileName(executing.Location) else "");
                (if calling <> null then Path.GetFileName(calling.Location) else "");
            |] 
            |> Seq.distinct |> Seq.toArray
        let asm = Array.append assemblies defaultAssemblies 
                 |> Array.toSeq
                 |> Seq.filter ((<>)"")
                 |> Seq.distinct
                 |> Seq.toArray

        ok asm

    let getStartupScript() =
        try
            let sourceCode = File.ReadAllText("Startup.fsx")
            ok sourceCode
        with
        | exn -> fail(sprintf "Failed to load source code from the starup script. %s" exn.Message)

    let compileScript(state : StartupScriptState) =
        try
            let provider = new FSharpCodeProvider()
            let params'= CompilerParameters()
            params'.GenerateExecutable <- false
            params'.OutputAssembly <- IO.Path.Combine(System.Environment.CurrentDirectory, outputAssembly)
            params'.IncludeDebugInformation <- true
            params'.ReferencedAssemblies.AddRange(state.referencedAssemblies)
            params'.GenerateInMemory <- false
            params'.WarningLevel <- 3
            params'.TreatWarningsAsErrors <- false

            let compiledResults = provider.CompileAssemblyFromSource(params', state.source)

            if compiledResults.Errors.Count > 0 then
                let errors = Array.init<CompilerError> compiledResults.Errors.Count (fun(_)-> new CompilerError())
                compiledResults.Errors.CopyTo(errors, 0)
                let errorMessage = seq { for error in errors do yield error.ErrorText} |> Seq.toArray   
                let errorMessages = Array.append [|"Failed to compile startup script."|] errorMessage
                fail(String.Join("\n", errorMessages))
            else
                let asm = Some(compiledResults.CompiledAssembly)
                ok { state with compiled = asm}
        with
        | exn -> fail(sprintf "Failed to compile startup script. %s" exn.Message)

    let invokeStartupScript(state : StartupScriptState) =
        try
            match state.compiled with
            | None -> fail(sprintf "Compile code has not been craeted.")
            | Some assembly ->  let startMethod  = assembly.GetType("Cronix.Startup.RunAtStartup").GetMethod("start")
                                let startInvoke (scheduler : IScheduleManager) = startMethod.Invoke(null, [|box (scheduler:IScheduleManager);|]) :?> unit
                                startInvoke(state.scheduleManager)
                                ok state
        with
            | exn -> fail(sprintf "Failed to run startup script. %s" exn.Message)
    
    let invokeStartupHandler (state: StartupHandlerState) =
        match state.startupHandler with
        | None -> ok state
        | Some handler -> 
            try
                handler.Invoke(state.scheduleManager)
                ok state
            with
            | exn -> fail(sprintf "Failed to run a startup handler. %s" exn.Message)

    let setupService(scheduleManager : IScheduleManager) (startupHandler : Option<StartupHandler>) =
        let startupScriptState scheduleManager referencedAssemblies  source = 
            { StartupScriptState.scheduleManager = scheduleManager; 
              referencedAssemblies = referencedAssemblies;
              source = source; 
              compiled = None;
            }
        let startupHandlerState scheduleManager startupHandler = 
            { StartupHandlerState.scheduleManager = scheduleManager; startupHandler = startupHandler;}

        startupHandlerState
        <!> ok scheduleManager
        <*> ok startupHandler
        >>= invokeStartupHandler
        |> logResult
        |> ignore

        if File.Exists(startupFile) = true then
            startupScriptState
            <!> ok scheduleManager
            <*> loadAssemblies()
            <*> getStartupScript()
            >>= compileScript
            >>= invokeStartupScript
            |> logResult 
            |> ignore

    let runService(startupHandler : Option<StartupHandler>) debug =
        try
            AppDomain.CurrentDomain.UnhandledException.AddHandler(fun(sender:obj) (args:UnhandledExceptionEventArgs) -> logger.Fatal(args.ExceptionObject))
            let scheduleManager = new ScheduleManager() :> IScheduleManager
            if debug = true then
                scheduleManager.Start()
                setupService scheduleManager startupHandler |> ignore
                Console.Read() |> ignore
                scheduleManager.Stop()
            else
                let setup() = 
                    setupService scheduleManager startupHandler |> ignore 
                use processAdapter = new ProjectInstaller.ServiceProcessAdapter(scheduleManager, setup)
                ServiceBase.Run(processAdapter)
                
        with
        | exn ->  logger.Fatal(exn)

    let printGuide() =
        let entryAsm = Assembly.GetEntryAssembly()
        let executingAsm =  Assembly.GetExecutingAssembly()
        let assemblyName = if entryAsm <> null then entryAsm.GetName().Name else executingAsm.GetName().Name
        printfn "Ussage:"
        printfn " %s debug" assemblyName
        printfn "    Starts the service in interactive mode."
        printfn "  %s install" assemblyName
        printfn "     Installs %s as a Windows service." assemblyName
        printfn "  %s uninstall" assemblyName
        printfn "     Uninstalls %s as a Windows service." assemblyName
    
    let isDebug() = Environment.UserInteractive
 
    let parseOption (input : Option<'a>) =
        match input with
        | None -> None
        | Some(null) -> None
        | Some(a') -> Some(a')

    let InitService : InitService =
        fun (args, startupHandler) ->
           
            let args' = parseOption args
            let startupHandler' = parseOption startupHandler
            
            if args'.IsNone || (args'.IsSome && args'.Value.Length = 0) then 
                runService startupHandler' <| isDebug()
                ok("runService")

            else if args'.IsSome && args'.Value.Length = 1 then
                match args'.Value.[0] with
                | "install" -> ProjectInstaller.Install(Assembly.GetEntryAssembly())
                | "uninstall" -> ProjectInstaller.Uninstall(Assembly.GetEntryAssembly())
                | "debug" -> runService startupHandler' <| isDebug()
                             ok("runService")
                | _ -> printGuide()
                       ok("printGuide") 
            else
               printGuide()
               ok("printGuide")