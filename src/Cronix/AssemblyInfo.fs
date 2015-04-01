namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Cronix")>]
[<assembly: AssemblyProductAttribute("cronix")>]
[<assembly: AssemblyDescriptionAttribute("Cron Service")>]
[<assembly: AssemblyVersionAttribute("0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1"
