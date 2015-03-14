namespace BootStrapperTests

open FsUnit.Xunit
open Xunit
open Cronix
open Chessie.ErrorHandling
open System.Reflection
open System.ServiceProcess
open System
open System.Collections.Generic
open System.Diagnostics 

//   [MethodName_StateUnderTest_ExpectedBehavior]

type DummyType() = 
    class
    end

module ServiceEnviroment =
    open System
    open System.IO
    open System.Diagnostics

    let sourcePath = "../../../../docs/samples/CSharpSample/bin/debug"
    let enviromentPath = "../../../../envs"
    let executable = "CSharpSample.exe"
 
    let rec deleteDirectory targetPath =
        let target = new DirectoryInfo(targetPath)
        if target.Exists then
            seq { for directory in target.EnumerateDirectories() -> directory }
            |> Seq.iter(fun(dir) -> deleteDirectory dir.FullName)

    let copyExecutable source target = 
        let rec copyAll (source : DirectoryInfo) (target : DirectoryInfo) =
            if target.Exists = false then
                target.Create() |> ignore
            for file in source.GetFiles() do
                let path = Path.Combine(target.FullName, file.Name)
                file.CopyTo(path, true) |> ignore
            for dirSourceSub in source.GetDirectories() do
                let nexttargetSubDir = target.CreateSubdirectory(dirSourceSub.Name)     
                copyAll dirSourceSub nexttargetSubDir

        let copy source target =
            let dirSource = new DirectoryInfo(source)
            let dirTarget = new DirectoryInfo(target)
            copyAll dirSource dirTarget

        copy source target

    let getTargetPath enviromentName = 
        let uri = new Uri(Assembly.GetAssembly(typedefof<DummyType>).CodeBase)
        let location = Path.GetDirectoryName(uri.LocalPath)
        let targetPath = Path.Combine(location, enviromentPath, enviromentName)
        let target = new DirectoryInfo(targetPath)
        target.FullName

    let createEnviroment enviromentName =
        let targetPath = getTargetPath enviromentName
        deleteDirectory targetPath
        copyExecutable sourcePath targetPath
     
    let createEnviromentForRollbackWithFailure =
        let name = "rollback"
        createEnviroment name
        File.Delete("FSharp.Scheduler.dll")
        name
    
    let isServiceInstalled serviceName =
        let service = ServiceController.GetServices() |> Array.tryFind(fun(s: System.ServiceProcess.ServiceController) -> s.ServiceName = serviceName)
        match service with
        | Some s -> true
        | _ -> false

    let uninstallService() =
        if isServiceInstalled "CSharpSample" then
            let process' = new Process()
            let startInfo = new ProcessStartInfo()
            startInfo.WindowStyle <- System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName <- "cmd.exe";
            startInfo.Arguments <- "/C sc delete CSharpSample";
            process'.StartInfo <- startInfo;
            process'.Start() |> ignore
 
open ServiceEnviroment   

type InitServiceTest() =
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

    interface IDisposable with
        member x.Dispose() =
            if process' <> null then
                if process'.HasExited = false then 
                    process'.Kill()|> ignore

            uninstallService()

module initServiceTest2 =
    open System
    open System.Collections.Generic
    open ServiceEnviroment
    open System.Diagnostics

   

   

// class
// setup
// desctructor
// mutable process; kill on dispose if posssible
// common results
// create process, with no use just let
// move event to create class
// move setup(Craete) to test class;
// let exit()
       
 
//    [<Fact>]
//    let ``Run as a service``() =
//        ()
//
//    [<Fact>]
//    let ``Install service``() =
//        BootStrapper.InitService(Some([|"install"|]), None) |> findOk <| "installed" |> ignore
//
//    [<Fact>]
//    let ``Uninstall service``() =
//        BootStrapper.InitService(Some([|"uninstall"|]), None) |> findOk <| "uninstalled" |> ignore
//        
//    [<Fact>]
//    let ``Print guide``() =
//        BootStrapper.InitService(Some([|""|]), None) |> findOk <| "printGuide" |> ignore
//        BootStrapper.InitService(Some([||]), None) |> findOk <| "printGuide" |> ignore






//module BootStrapperTests =
//
//    [<Test>]
//    let ``loadAssemblies returns a list of assemblies for executed assembly``() =
//        ()
//
//    [<Test>]
//    let ``getStartupScript loads script``() = 
//        ()
//
//    [<Test>]
//    let ``getStartupScript throws exception when script was not found``() = 
//        ()
//
//
//    [<Test>]
//    let ``compileScript returns compiled assembly``() =
//        ()
//
//    [<Test>]
//    let ``compileScript returns error messages when compilation failed``() =
//        ()
//
//
//    [<Test>]
//    let ``invokeStartupScript invokes startup script``() =
//        ()
//
//    [<Test>]
//    let ``invokeStartupScript fails when invoking startup script throws exception``() =
//        ()
//
//    [<Test>]
//    let ``invokeStartupScript fails when code is not compiled``() =
//        ()
//
//    [<Test>]
//    let ``invokeStartupHandler``() =
//        ()
//
//    [<Test>]
//    let ``setupService``() =
//        ()
//    
//    [<Test>]
//    let ``runService``() = 
//        ()
//
