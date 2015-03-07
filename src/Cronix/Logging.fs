module Logging

open NLog
open System
open Chessie.ErrorHandling

let logger() = 
    let caller = System.Diagnostics.StackTrace(1, false).GetFrames().[1].GetMethod().DeclaringType
    LogManager.GetLogger(caller.Name)

let logResult result : Result<_,string> = 

     let caller = System.Diagnostics.StackTrace(1, false).GetFrames().[0].GetMethod().DeclaringType
     let logger = LogManager.GetLogger(caller.Name)

     result 
        |> successTee (fun (obj, msgs) -> msgs |> List.iter (fun (m:string) -> logger.Debug(m)))
        |> ignore
     result 
        |> failureTee (fun (msgs) -> msgs |> List.rev |> List.iter (fun (m:string) -> logger.Error(m)))  
        |> ignore
     result