namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Cronix")>]
[<assembly: AssemblyProductAttribute("Cronix")>]
[<assembly: AssemblyDescriptionAttribute("F# / C# Cron Service")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
