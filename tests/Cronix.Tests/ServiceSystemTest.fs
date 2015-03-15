namespace Cronix.Tests

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

[<Trait("Service", "Unit Test")>]
type SetupServiceTest() =
    
    [<Fact>]
    let ``setup service returns result`` = failwith "Not Implemented."

    [<Fact>]
    let ``invoke startup handler returns success``() = failwith "Not Implemented."

    [<Fact>]
    let ``invoke startup handler returns failure``() = failwith "Not Implemented."

    [<Fact>]
    let ``load assemblies returns an array of dll names``() = failwith "Not Implemented."

    [<Fact>]
    let ``get startup script returns success``() = failwith "Not Implemented."

    [<Fact>]
    let ``get startup script returns failure``() = failwith "Not Implemented."

    [<Fact>]
    let ``comlipe script returns success``() = failwith "Not Implemented."

    [<Fact>]
    let ``compile script returns failure``() = failwith "Not Implemented."

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
                
        process' <- Process.Start(startInfo)
        process'.BeginOutputReadLine() |> ignore
        process'.OutputDataReceived.Add(fun(args) -> 
            Console.WriteLine("event: {0} {1}", args.Data, DateTime.Now)
            result.Add(args.Data))     
                            
    let exit() = 
        process'.StandardInput.WriteLine("exit") |> ignore
            
    let wait() = process'.WaitForExit() |> ignore

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

 