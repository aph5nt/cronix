(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
Defining and configuring jobs with startup.fsx file.

The startup script manual
========================

Introduction: What is startup.fsx file for?

The intention of having a seperate file for configuring jobs, is to give developers the flexibility and ease of use.
In the startup.fsx file, developer can configure and schedule jobs embeded in the service assembly and also reference 
external assemblies and create custom jobs on fly!
Startup script is compiled on the Cronix service startup time.

Sample script:

- Lets take a look how does the minimalistic startup.fsx file looks like.
*)
 

// We reference assemblies for Visual Studio intellisense support.
#if INTERACTIVE
#I "."
#I "bin\Debug"
#I "bin\Release"
#r "Cronix.dll"
#r "CSharpSample.exe" // this is our executable where we had initialized Cronix service.
#r "Chessie.dll"
#endif

/// This namespace is required.
namespace Cronix.Startup

/// This module is required.
module RunAtStartup =
    
    open System.Threading
    open System
    open Cronix
    open CSharpSample.Jobs

    /// startup method
    let start (scheduler : IScheduleManager) =
        scheduler.Schedule "my job name" "* * * * *" <| Callback(sampleJob) |> ignore
        ()

(**
and more complex example...
*)

#if INTERACTIVE
#I "."
#I "bin\Debug"
#I "bin\Release"
#r "Cronix.dll"
#r "CSharpSample.exe"
#r "CSharpSample.Jobs.dll"
#r "Chessie.dll"
//#r "YourTask.dll"
#endif

namespace Cronix.Startup

module RunAtStartup =
    
    open System.Threading
    open System
    open Cronix

    //reference yours custom assembly here
    open CSharpSample
    open CSharpSample.Jobs

    // inline job
    let sampleJob (token : CancellationToken) = 
      printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
      Thread.Sleep 100

    let start (scheduler : IScheduleManager) =
        scheduler.Schedule "inline job" <| "* * * * *" <| Callback(sampleJob) |> ignore
        scheduler.Schedule "job from external assembly" <|  "* * * * *" <| Callback(ExternalJobs.Callback) |> ignore
        scheduler.Schedule "job defined inside the cronix service" <|  "* * * * *" <| Callback(EmbededJobs.Callback) |> ignore
        scheduler.Schedule "with frequency helper" <| frequency Hourly <| Callback( sampleJob ) |> ignore
        ()

(**
Adding Startup.fsx file to the Cronix service

- Just create new Startup.fsx file inside your Cronix project root directory, type your code there (use one of these samples) and change file property 'Copy to Output Directory' to 'Copy always'.
*)
