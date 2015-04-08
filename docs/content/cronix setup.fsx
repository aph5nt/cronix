(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

(**
CRONIX service setup
========================

- create console application
- reference Cronix nuget package
- bootstrapp Cronix service as in sample below

*)

(** C# Console application *)

using Chessie.ErrorHandling.CSharp;
using Cronix;
using Microsoft.FSharp.Core;
using System;
using System.Linq;
using System.Threading;

namespace CSharpSample
{
	public static class EmbededJobs
	{
		public static void Callback(CancellationToken token)
		{
			Console.WriteLine("executing embeded job from CSharpSample.dll at {0}", DateTime.Now);
			Thread.Sleep(100);
		}
	}

	public class Program
	{
		public static void Main(string[] args)
		{
			var result = BootStrapper.InitService(new FSharpOption<string[]>(new[] { "debug" }),
				new FSharpOption<StartupHandler>(scheduler =>
				{
					// schedule your job here
					scheduler.Schedule("scheduled job", "* * * * *", EmbededJobs.Callback);
				}));
			result.Match(
				(state, msgs) =>
				{
					Console.WriteLine(state);
                },
				(msgs) =>
				{
					msgs.ToList().ForEach(Console.WriteLine);
				});
		}
	}
}

(** F# Console application *)

open System
open Cronix
open Chessie.ErrorHandling
open System.Threading

let sampleJob (token : CancellationToken) = 
      printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
      Thread.Sleep 100
 
[<EntryPoint>]
let main argv = 
 
    let startupHandler = 
        new StartupHandler(
            fun(scheduler) ->
                        scheduler.Schedule "scheduled job"  <| "* * * * *" <| Callback( sampleJob ) |> ignore
            )

    let result = BootStrapper.InitService(Some(argv), Some(startupHandler))
    match result with
    | Ok (state, msgs) -> printfn "%s" state
    | Fail msgs -> msgs |> List.iter(fun(s) ->  printfn "%s" s)
    | _ -> ()
    0 // return an integer exit code