namespace BootStrapperTests
open FsUnit
open NUnit.Framework
open Cronix
open Chessie.ErrorHandling

//   [MethodName_StateUnderTest_ExpectedBehavior]

//todo
// - enviroments
// fix unit test runner! 2.6.3 nunit is fine, but i reference 2.6.4

// enviroment
// copy CharpSample to location
// invoke manipulation

module ServiceEnviroment =
    open System.IO

    let sourcePath = ""
 
    let rec deleteDirectory (target : DirectoryInfo) =
        seq { for directory in target.EnumerateDirectories() -> directory }
        |> Seq.iter(fun(dir) -> deleteDirectory dir)

    let copyExecutable source target = 
        let rec copyAll (source : DirectoryInfo) (target : DirectoryInfo) =
            if Directory.Exists target.FullName then
                Directory.CreateDirectory target.FullName |> ignore
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

    let createEnviroment targetPath = 
        let target = new DirectoryInfo(targetPath)
        deleteDirectory target
        copyExecutable sourcePath targetPath
     
    let createEnviromentForRollbackWithFailure =
        createEnviroment "rollback"
        File.Delete("FSharp.Scheduler.dll")


    let runBootStrapper = 
        // load assemblies
        // FSharpCodeProvider -> load exec
        // cast BootStrapper
        // invoke bootstrapper!
        // read output!
        ()

module initServiceTest =

    let findOk result item = 
        result 
        |> successTee (fun (obj, msgs) ->
            let found = msgs |> List.toSeq |> Seq.tryFind (fun(m) -> m = item)
            match found with
            | Some msg -> msg |> should be (sameAs item)
            | None -> failwith(sprintf "%s not found" item)
        )

    let findFailure result item = 
        result 
        |> failureTee (fun (msgs) ->
            let found = msgs |> List.toSeq |> Seq.tryFind (fun(m) -> m = item)
            match found with
            | Some msg -> msg |> should be (sameAs item)
            | None -> failwith(sprintf "%s not found" item)
        )
 
  
    [<Test>]
    let ``Run in debug mode``() =
        BootStrapper.InitService(Some([|"debug"|]), None) |> findOk <| "runService"

    [<Test>]
    let ``Run as a service``() =
        ()

    [<Test>]
    let ``Install service``() =
        BootStrapper.InitService(Some([|"install"|]), None) |> findOk <| "installed" |> ignore

    [<Test>]
    let ``Uninstall service``() =
        BootStrapper.InitService(Some([|"uninstall"|]), None) |> findOk <| "uninstalled" |> ignore
        
    [<Test>]
    let ``Print guide``() =
        BootStrapper.InitService(Some([|""|]), None) |> findOk <| "printGuide" |> ignore
        BootStrapper.InitService(Some([||]), None) |> findOk <| "printGuide" |> ignore






module BootStrapperTests =

    [<Test>]
    let ``loadAssemblies returns a list of assemblies for executed assembly``() =
        ()

    [<Test>]
    let ``getStartupScript loads script``() = 
        ()

    [<Test>]
    let ``getStartupScript throws exception when script was not found``() = 
        ()


    [<Test>]
    let ``compileScript returns compiled assembly``() =
        ()

    [<Test>]
    let ``compileScript returns error messages when compilation failed``() =
        ()


    [<Test>]
    let ``invokeStartupScript invokes startup script``() =
        ()

    [<Test>]
    let ``invokeStartupScript fails when invoking startup script throws exception``() =
        ()

    [<Test>]
    let ``invokeStartupScript fails when code is not compiled``() =
        ()

    [<Test>]
    let ``invokeStartupHandler``() =
        ()

    [<Test>]
    let ``setupService``() =
        ()
    
    [<Test>]
    let ``runService``() = 
        ()

