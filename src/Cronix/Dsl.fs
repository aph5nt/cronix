namespace Cronix

module Dsl = 

    open System
    open System.Collections.Concurrent
    open System.Threading
    open System.Threading
    open NCrontab 
    open System.Collections.Concurrent
    open System

    type CronExpr =
        | Hourly
        | Daily
        | Weekly
        | Monthly

    let crontab expr = 
        match expr with
        | Hourly -> "01 * * * *"
        | Daily -> "02 4 * * *"
        | Weekly -> "22 4 * * 0"
        | Monthly -> "42 4 1 * *"

    crontab Daily |> printf "%s" 



(* DSL language *)
////let schedule (name: string) (task: JobType) (expr: string) =
////    let taskEntry = { 
////        TimeTableEntry.Name = name;
////        CronExpr = expr;
////        Job = task;
////        NextOccurance =  // Services.CalculateOccurence expr
////    }
////    match Services.ScheduleTask taskEntry TimeTable with
////    | Failure err -> printf "%s" err
////    | Success _ -> printf "Task '%s' has been scheduled." name