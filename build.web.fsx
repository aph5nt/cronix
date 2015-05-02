// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System
open System.IO
#if MONO
#else
#load "packages/SourceLink.Fake/tools/Fake.fsx"
open SourceLink
#endif

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "cronix web"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Cronix Web Interface"

// Longer description of the project
// (used as a description for NuGet package; line breaks are automatically cleaned up)
let description = "Cronix Web Interface"

// List of author names (for NuGet package)
let authors = [ "aph5nt" ]

// Tags for your project (for NuGet package)
let tags = "cronix ui web cron ncron ncrontab scheduler service"

// File system information 
let solutionFile  = "Cronix.Web.sln"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "aph5nt" 
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/aph5nt"
let gitName = "Cronix"
// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES_WEB.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|) (projFileName:string) = 
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath, 
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
     
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (("src" @@ folderName) @@ "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName @@ "Properties") @@ "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName @@ "My Project") @@ "AssemblyInfo.vb") attributes
        )
)

// Copies binaries from default VS location to exepcted bin folder
// But keeps a subdirectory structure for each project in the 
// src folder to support multiple project outputs
Target "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) @@ "bin/Release", "bin" @@ (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    !! solutionFile
    |> MSBuildRelease "" "Rebuild"
    |> ignore
)

#if MONO
#else
// --------------------------------------------------------------------------------------
// SourceLink allows Source Indexing on the PDB generated by the compiler, this allows
// the ability to step through the source code of external libraries https://github.com/ctaggart/SourceLink

Target "SourceLink" (fun _ ->
    let baseUrl = sprintf "%s/%s/{0}/%%var2%%" gitRaw (project.ToLower())
    use repo = new GitRepo(__SOURCE_DIRECTORY__)
    
    let addAssemblyInfo (projFileName:String) = 
        match projFileName with
        | Fsproj -> (projFileName, "**/AssemblyInfo.fs")
        | Csproj -> (projFileName, "**/AssemblyInfo.cs")
        | Vbproj -> (projFileName, "**/AssemblyInfo.vb")
        
    !! "src/**/*.??proj"
    |> Seq.map addAssemblyInfo
    |> Seq.iter (fun (projFile, assemblyInfo) ->
        let proj = VsProj.LoadRelease projFile 
        logfn "source linking %s" proj.OutputFilePdb
        let files = proj.Compiles -- assemblyInfo
        repo.VerifyChecksums files
        proj.VerifyPdbChecksums files
        proj.CreateSrcSrv baseUrl repo.Revision (repo.Paths files)
        Pdbstr.exec proj.OutputFilePdb proj.OutputFilePdbSrcSrv
    )
)

#endif

// --------------------------------------------------------------------------------------
// Build a Web UI

Target "BuildWebUI" (fun _ ->
    ExecProcessElevated "gulp"  "" <| TimeSpan.FromSeconds(60.0) |> ignore
    //shellExec({ ExecParams.WorkingDirectory = "src/Cronix.UI"; Program = "gulp"; ExecParams.Args = []; ExecParams.CommandLine="" }) |> ignore
)


// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "NuGet" (fun _ ->
    Paket.Pack(fun p -> 
        { p with    
            OutputPath = "bin"
            Version = release.NugetVersion
            ReleaseNotes = toLines release.Notes})
)

Target "PublishNuget" (fun _ ->
    Paket.Push(fun p -> 
        { p with
            WorkingDir = "bin";
            ApiKey = System.IO.File.ReadAllText(@"c:\workplace\nuget.key"); })
)

#load "paket-files/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
    
    // release on github
    createClient (getBuildParamOrDefault "github-user" "") (getBuildParamOrDefault "github-pw" "")
    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes 
    // TODO: |> uploadFile "PATH_TO_FILE"    
    |> releaseDraft
    |> Async.RunSynchronously
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "All"

"All" 
#if MONO
#else
  =?> ("SourceLink", Pdbstr.tryFind().IsSome )
#endif
  ==> "BuildWebUI"
  ==> "NuGet"
  ==> "BuildPackage"

"BuildPackage"
  ==> "PublishNuget"
  ==> "Release"

Target "UI" DoNothing

"UI"
==> "BuildWebUI"

RunTargetOrDefault "All"
