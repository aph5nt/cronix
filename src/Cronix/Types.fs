namespace Cronix

open System
open System.Threading
open System.Collections.Generic
open Chessie.ErrorHandling
open System.Reflection

(* Triggers *) 
/// Describers the trigger state.
type TriggerState =
    | Stopped
    | Idle
    | Executing
    | Terminated 

/// Describers the trigger options.
and TriggerOption =
    | RescheduleAfterFailure
    | Overlap 

/// Describes the trigger.
and ITrigger =
    /// Returns the trigger state.
    abstract member State: TriggerState with get

    /// Returns the occurance time.
    abstract member OccurrenceAt: DateTime with get

    /// Starts the trigger.
    abstract member Start: unit -> unit

    /// Stops the trigger.
    abstract member Stop: unit -> unit

    /// Terminates the trigger execution.
    abstract member Terminate: unit -> unit

    /// Fires the trigger.
    abstract member Fire: unit -> unit

/// The trigger callback delegate
and Callback = delegate of (CancellationToken) -> unit

/// Calculates the timer argument function signature. 
and CalculateTimerArgs = JobCronExpr * DateTime * DateTime -> ChangeArgs

/// The change time argument type
and ChangeArgs = int * int 
 
/// The calculates the occurance at time function signature.
and CalculatetOccurrenceAt = JobCronExpr * DateTime -> DateTime

/// The occurance at type
and OccurrenceAt = DateTime

/// The trigger callback delegate.
and TimerCallback = JobName * Callback * CancellationToken -> unit

/// The trigger service facade.
and TriggerServices = {
    calculateTimerArgs : CalculateTimerArgs
    calculatetOccurrenceAt : CalculatetOccurrenceAt
    timerCallback : TimerCallback
}

/// Creates new trigger function signature.
and CreateTrigger = JobName * JobCronExpr * Callback  -> ITrigger

/// The trigger factory.
and TriggerFactory = {
    create: CreateTrigger
}

(* Jobs *)
/// The job type.
and Job = {
    Name : JobName;
    CronExpr : JobCronExpr;
    Trigger : ITrigger;
}

/// The job name.
and JobName = string

/// The cron expression type.
and JobCronExpr = string

/// The job state type.
and JobState = {
    Name : JobName;
    CronExpr : JobCronExpr;
    TriggerState : TriggerState
}

(* Scheduling *)
/// The schedule function signature.
type Schedule = ScheduleState * ScheduleParams -> Result<ScheduleState, string>

/// The unschedule function signature.
and UnSchedule = ScheduleState * JobName ->  Result<ScheduleState, string>

/// The start function signature.
and Start = ScheduleState * JobName ->  Result<ScheduleState, string>

/// The stop function signature.
and Stop = ScheduleState * JobName ->  Result<ScheduleState, string>

/// The schedule state type.
and ScheduleState = Dictionary<string, Job>

/// The schedule params type.
and ScheduleParams = JobName * JobCronExpr * Callback

/// The schedule services facade.
and ScheduleServices = {
    schedule : Schedule
    unschedule : UnSchedule
    start : Start
    stop : Stop
}

/// Describes the schedule mannager commands.
and ScheduleManagerCommand = 
    | ScheduleJob of JobName * JobCronExpr * Callback
    | UnScheduleJob of JobName
    | StopJob of JobName
    | StartJob of JobName

/// Manges the job schedules.
and IScheduleManager = 
    inherit IDisposable
    /// Schedules new job.
    abstract member Schedule: string -> string -> Callback -> Result<ScheduleState, string>

    /// Unschedules given job.
    abstract member UnSchedule: string -> Result<ScheduleState, string>

    /// Starts given job.
    abstract member StartJob : string -> Result<ScheduleState, string>

    /// Stops given job.
    abstract member StopJob : string -> Result<ScheduleState, string>

    /// Returns jobs state.
    abstract member State: seq<JobState>

    /// Stops schedule manager.
    abstract member Stop: unit -> unit

    /// Starts schedule manager.
    abstract member Start: unit -> unit

/// Describes result message.
type ResultMessage = 
    | FailureMessage of FailureMessage
    | SuccessMessage of SuccessMessage

/// Describes the failure message.
and FailureMessage =
    | InvalidCronExpr
    | JobExists
    | JobNotExists

/// Describes the sucess message.
and SuccessMessage = 
    | ValidateExprMsg
    | CanAddJobMsg
    | AddJobMsg
    | JobFoundMsg
    | RemoveJobMsg
    | StopJobMsg
    | StartJobMsg


(* Installer *)
/// The install function signature.
type Install = Assembly -> Result<string, string>

/// The uninstall function signature.
type Uninstall = Assembly -> Result<string, string>


(* BootStrapper *)
/// The startup handler state type.
type StartupHandlerState = {
    scheduleManager : IScheduleManager
    startupHandler : Option<StartupHandler>
}

/// The startup script state type.
and StartupScriptState = {
        scheduleManager : IScheduleManager
        referencedAssemblies : string[]
        source : string
        compiled : Option<Assembly>
}

/// The startup handler delegate.
and StartupHandler = delegate of (IScheduleManager) -> unit

/// The run service function signature.
and RunService = IScheduleManager -> Option<StartupHandler> -> unit

/// The initialize service function signature.
and InitService = Option<string[]> * Option<StartupHandler> -> Result<string, string>

(* Messages *)
/// Module responsible for generic messages.
[<AutoOpen>]
module Messages = 

    /// Returns the failure messages.
    let FailureMessages : Map<FailureMessage, string> =
        Map.empty.
            Add(InvalidCronExpr, "Expr <{0}> is not valid.")
            .Add(JobExists, "Job <{0}> already exists.")
            .Add(JobNotExists, "Job <{0}> does not exists.")

    /// Returns the success messages.
    let SuccesMessagess : Map<SuccessMessage, string> =
        Map.empty.
            Add(ValidateExprMsg, "Expr <{0}> is valid.")
            .Add(CanAddJobMsg, "Job <{0}> can be added.")
            .Add(AddJobMsg, "Job <{0}> has been added.")
            .Add(JobFoundMsg, "Job <{0}> found.")
            .Add(RemoveJobMsg, "Job <{0}> has been removed.")
            .Add(StopJobMsg, "Job <{0}> has been stopped.")
            .Add(StartJobMsg, "Job <{0}> has been started.")
        
    /// Creates the failure message.
    let FailureMessage (key : FailureMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(FailureMessages.[key], params')
        Fail [result]

    /// Creates the sucess message.
    let SuccesMessage state (key : SuccessMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(SuccesMessagess.[key], params')
        Ok(state, [result])
     
[<AutoOpen>]
module Dsl = 

    /// Describes the frequence rate
    type CronExpr =
        /// 01 * * * *
        | Hourly

        /// 02 4 * * *
        | Daily

        /// 22 4 * * 0
        | Weekly

        /// 42 4 1 * *
        | Monthly

    let frequency expr = 
        match expr with
        | Hourly -> "01 * * * *"
        | Daily -> "02 4 * * *"
        | Weekly -> "22 4 * * 0"
        | Monthly -> "42 4 1 * *"