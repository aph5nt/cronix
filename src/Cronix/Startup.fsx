(*
Best practices:
- Design startup script with visual studio
- reference your dll with jobs for F# interactive (while editing this script)
- remember to open(import) your job dll inside RunAtStartup module
*)

#if INTERACTIVE
#I "."
#I "bin\Debug"
#I "bin\Release"
#r "Cronix.dll"
#r "Chessie.dll"
//#r "YourTask.dll"
#endif

namespace Cronix.Startup

module RunAtStartup =
    
    open System.Threading
    open System
    open Cronix

    //open YourJob.dll must be opened here!
    
    // sample job
    let sampleJob (token : CancellationToken) = 
      printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
      Thread.Sleep 100


    // open your dll with tasks here
    let start (scheduler : IScheduleManager) =

        // schedules goes here!
        scheduler.Schedule "job1" "* * * * *" <| JobCallback(sampleJob) |> ignore
        ()