#if INTERACTIVE
#I "."
#I "bin\Debug"
#I "bin\Release"
#r "Cronix.dll"
#r "FSharpSample.exe"
#r "Chessie.dll"
#endif

namespace Cronix.Startup

module RunAtStartup =
    
    open System.Threading
    open System
    open Cronix
    open Messages

    let sampleJob (token : CancellationToken) = 
      printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
      Thread.Sleep 100

    let start (scheduler : IScheduleManager) =
        scheduler.Schedule "job1" "* * * * *" <| Callback(sampleJob) |> ignore       
        ()