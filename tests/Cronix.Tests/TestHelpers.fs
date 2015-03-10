module TestHelpers

    open FsUnit
    open NUnit.Framework
    open System.Threading   
    open Chessie.ErrorHandling
    open System

    let callback (token : CancellationToken) = 
        printf "callback executed at (UTC) %s\n" <| DateTime.UtcNow.ToString()
        Thread.Sleep 100

    let compareResults (actual : Result<'TSuccess, 'TMessage>) (expected : Result<'TSuccess, 'TMessage>)  =
        actual |> should equal expected

