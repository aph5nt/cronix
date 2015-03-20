﻿namespace Cronix.Tests

open FsUnit.Xunit
open Xunit
open Cronix
open Chessie.ErrorHandling
open System.Reflection
open System.ServiceProcess
open System
open System.Collections.Generic
open System.Diagnostics 
open ServiceEnviroment   
open BootStrapper
open System.IO

[<Trait("Service", "Unit Test")>]
type SetupServiceTest() =
    
    [<Fact>]
    let ``setup service returns result``() = failwith "Not Implemented."

    [<Fact>]
    let ``invoke startup handler returns success``() = failwith "Not Implemented."

    [<Fact>]
    let ``invoke startup handler returns failure``() = failwith "Not Implemented."

    [<Fact>]
    let ``load assemblies returns an array of dll names``() = 
        loadAssemblies()
        |> successTee (fun(obj : string[], _) -> obj |> Array.iter (fun(m:string) -> Console.WriteLine(m)))
        |> ignore
        ()

    [<Fact>]
    let ``get startup script returns success``() = 
        if IO.File.Exists("Startup.fsx.bak") then  IO.File.Move("Startup.fsx.bak", "Startup.fsx")
        match getStartupScript() with
         | Ok (script, msgs) -> script |> should not' NullOrEmptyString
         | Fail msgs -> failwith "Expected Success Tee"


    [<Fact>]
    let ``get startup script returns failure``() = 
        if IO.File.Exists("Startup.fsx") then IO.File.Move("Startup.fsx", "Startup.fsx.bak")
        match getStartupScript() with
         | Ok (_, _) -> failwith "Expected Failure Tee"
         | Fail msgs -> IO.File.Move("Startup.fsx.bak", "Startup.fsx")

        
    [<Fact>]
    let ``comlipe script returns failure when compilation have errors``() =

        let assemblies = 
            [|
                "Chessie.dll";
                "Cronix.Tests.dll";
                "Cronix.dll";
                "FSharp.Compiler.CodeDom.dll";
                "FSharp.Core.dll";
                "FsUnit.CustomMatchers.dll";
                "FsUnit.Xunit.dll";
                "NCrontab.dll";
                "NHamcrest.dll";
                "NLog.dll";
                "xunit.abstractions.dll";
                "xunit.dll";
                "xunit.runner.utility.desktop.dll";
                "xunit.runner.visualstudio.testadapter.dll";
                "System.dll";
            |]

        let result = compileScript({ source = IO.File.ReadAllText("Startup.fsx"); 
                        compiled = None;
                        referencedAssemblies = assemblies;
                        scheduleManager = new ScheduleManager() })

        match result with
        | Ok (_, _) -> failwith "Expected Failure Tee"
        | Fail msgs -> 
            msgs.[0] |> should startWith "Failed to compile startup script."
            msgs.[0].Contains("FS0229") |> should equal true
            msgs.[0].Contains("FS3160") |> should equal true

    [<Fact>]
    let ``compile script returns failure when exception is thrown``() = 
        let result = compileScript({ source = IO.File.ReadAllText("Startup.fsx"); 
                                     compiled = None;
                                     referencedAssemblies = [||];
                                     scheduleManager = new ScheduleManager() })
        match result with
         | Ok (_, _) -> failwith "Expected Failure Tee"
         | Fail msgs -> msgs.[0] |> should startWith "Failed to compile startup script."
                        msgs.[0]

    [<Fact>]
    let ``invoke startup script returns success``() = failwith "Not Implemented."

    [<Fact>]
    let ``invoke startup script returns failure``() = failwith "Not Implemented."

[<Trait("Service", "System Test")>]
type ServiceSystemTest() =
    let result = new List<string>()
    let mutable process' : Process = null
    do
        uninstallService()

    let createProcess (enviromentName) (parameter) = 
        let targetPath = getTargetPath enviromentName
        let executable = System.IO.Path.Combine(targetPath, executable)
        let startInfo = new ProcessStartInfo()
        startInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Normal
        startInfo.FileName <- executable
        startInfo.Arguments <- parameter
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardInput <- true
        startInfo.UseShellExecute <- false
        startInfo.UserName <- "cronix"
        startInfo.Password <- loadUserPassword()
              
        process' <- Process.Start(startInfo)
        process'.BeginOutputReadLine() |> ignore
        process'.OutputDataReceived.Add(fun(args) -> 
            Console.WriteLine("event: {0} {1}", args.Data, DateTime.Now)
            result.Add(args.Data))     
                            
    let exit() = 
        process'.StandardInput.WriteLine("exit") |> ignore
            
    let wait() = process'.WaitForExit() |> ignore

    let rec searchInLogFile message till =
        Async.Sleep 100 |> ignore
        if till > DateTime.UtcNow then
            let logName = sprintf "%s.log" <| DateTime.UtcNow.ToShortDateString()
            if File.Exists logName then
               let content = File.ReadAllText(logName)
               if content.Contains(message) = false then searchInLogFile message till
            else 
                searchInLogFile message till

    [<Fact>]
    let ``Run in debug mode``() =
        createEnviroment "debugEnv"
        createProcess "debugEnv" "debug"
        exit()
        wait()
        result |> should contain "runService"

    [<Fact>]
    let ``Install service``() =
        createEnviroment "serviceEnv"
        createProcess "serviceEnv" "install"
        wait()
        result |> should contain "installed"

    [<Fact>]
    let ``Run installed service``() =
        createEnviroment "serviceEnv"
        createProcess "serviceEnv" "install"
        wait()
        result |> should contain "installed"
        startService()
        searchInLogFile "callback executed at (UTC) " <| DateTime.UtcNow.AddMinutes 1.5

    [<Fact>]
    let ``Rollback instalation on installation failure``() =
        createEnviroment "serviceEnv"
        createProcess "serviceEnv" "install"
        wait()
        createProcess "serviceEnv" "install"
        wait()
        result |> should contain "rollback: The specified service already exists"

    [<Fact>]
    let ``Uninstall service``() =
        createEnviroment "serviceEnv"
        createProcess "serviceEnv" "install"
        wait()
        createProcess "serviceEnv" "uninstall"
        wait()
        result |> should contain "uninstalled" 

    [<Fact>]
    let ``Print guide for invalid param``() =
        createEnviroment "serviceEnv"
        createProcess "serviceEnv" "?"
        wait()
        result |> should contain "printGuide" 

    [<Fact>]
    let ``Print guide for null param``() =
        createEnviroment "serviceEnv"
        createProcess "serviceEnv" null
        wait()
        result |> should contain "printGuide" 

    interface IDisposable with
        member x.Dispose() =
            if process' <> null then
                if process'.HasExited = false then 
                    process'.Kill()|> ignore

            uninstallService()

 