namespace Cronix.Tests

open Xunit
open FsUnit.Xunit

module AppSettingsTests = 

    [<Fact>]
    let ``Read host port setting default``() =
        AppSettings.port 9090 |> should not' (equal 9090)
        
    [<Fact>]
    let ``Read host port setting``() =
        AppSettings.port 9090 |> should equal 8080