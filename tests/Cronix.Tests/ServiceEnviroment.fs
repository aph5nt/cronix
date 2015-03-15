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