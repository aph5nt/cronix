﻿namespace Cronix

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

/// Calculates the timer argument.
and CalculateTimerArgs = JobCronExpr * DateTime * DateTime -> ChangeArgs

/// The change time argument
and ChangeArgs = int * int 
 
/// The calculates the occurance at time.
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

/// Creates new trigger.
and CreateTrigger = JobName * JobCronExpr * Callback  -> ITrigger

/// The trigger factory.
and TriggerFactory = {
    create: CreateTrigger
}

(* Jobs *)
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
/// The schedule type.
type Schedule = ScheduleState * ScheduleParams -> Result<ScheduleState, string>

/// The unschedule type.
and UnSchedule = ScheduleState * JobName ->  Result<ScheduleState, string>

/// The start type.
and Start = ScheduleState * JobName ->  Result<ScheduleState, string>

/// The stop type.
and Stop = ScheduleState * JobName ->  Result<ScheduleState, string>

/// THe schedule state type.
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
    | Schedule of JobName * JobCronExpr * Callback
    | UnSchedule of JobName
    | Stop of JobName
    | Start of JobName

/// Manges the job schedules.
and IScheduleManager = 
    inherit IDisposable
    /// Schedules new job.
    abstract member Schedule: string -> string -> Callback -> Result<ScheduleState, string>

    /// Unschedules given job.
    abstract member UnSchedule: string -> Result<ScheduleState, string>

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
    | ValidateExpr
    | CanAddJob
    | AddJob
    | JobFound
    | RemoveJob
    | StopJob
    | StartJob


(* Installer *)
/// The install type.
type Install = Assembly -> Result<string, string>

/// The uninstall type type.
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

/// The startup handler type.
and StartupHandler = delegate of (IScheduleManager) -> unit

/// The run service type.
and RunService = IScheduleManager -> Option<StartupHandler> -> unit

/// The initialize service type.
and InitService = Option<string[]> * Option<StartupHandler> -> Result<string, string>

(* Messages *)
/// Module responsible for generic messages.
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
            Add(ValidateExpr, "Expr <{0}> is valid.")
            .Add(CanAddJob, "Job <{0}> can be added.")
            .Add(AddJob, "Job <{0}> has been added.")
            .Add(JobFound, "Job <{0}> found.")
            .Add(RemoveJob, "Job <{0}> has been removed.")
            .Add(StopJob, "Job <{0}> has been stopped.")
            .Add(StartJob, "Job <{0}> has been started.")
        
    /// Creates the failure message.
    let FailureMessage (key : FailureMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(FailureMessages.[key], params')
        Fail [result]

    /// Creates the sucess message.
    let SuccesMessage state (key : SuccessMessage) ([<ParamArray>] params' : list<_>) =
        let result = String.Format(SuccesMessagess.[key], params')
        Ok(state, [result])
     