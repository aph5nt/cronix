namespace Cronix

/// Module responsible for providing the Domain Specific language for creating crontab expressions
module Dsl = 

    open System
    open NCrontab 

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

    /// Changes frequence rate into crontab expression.
    let crontab expr = 
        match expr with
        | Hourly -> "01 * * * *"
        | Daily -> "02 4 * * *"
        | Weekly -> "22 4 * * 0"
        | Monthly -> "42 4 1 * *"

    crontab Daily |> printf "%s" 