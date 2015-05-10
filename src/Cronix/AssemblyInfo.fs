namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Cronix")>]
[<assembly: AssemblyProductAttribute("cronix")>]
[<assembly: AssemblyDescriptionAttribute("Cron Service")>]
[<assembly: AssemblyVersionAttribute("0.3.2")>]
[<assembly: AssemblyFileVersionAttribute("0.3.2")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3.2"
