#if INTERACTIVE
#I "."
#I "bin\Debug"
#I "bin\Release"
#r "Cronix.dll"
#r "Chessie.dll"
#endif

namespace Cronix.Startup

module RunAtStartup =
    
    open System.Threading
    open System
    open Cronix

    
    /// sample job
    let sampleJob (token : CancellationToken) = 
      printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
      Thread.Sleep 100

    /// open your dll with tasks here
    let start (scheduler : IScheduleManager) =

        // schedules goes here!
        scheduler.ScheduleJob "job1" "* * * * *" <| JobCallback(sampleJob) |> ignore
        ()
