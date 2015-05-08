namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Cronix.Web")>]
[<assembly: AssemblyProductAttribute("cronix")>]
[<assembly: AssemblyDescriptionAttribute("Cron Service")>]
[<assembly: AssemblyVersionAttribute("0.3.1")>]
[<assembly: AssemblyFileVersionAttribute("0.3.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3.1"
