namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Cronix.Web")>]
[<assembly: AssemblyProductAttribute("cronix web")>]
[<assembly: AssemblyDescriptionAttribute("Cronix Web Interface")>]
[<assembly: AssemblyVersionAttribute("0.3")>]
[<assembly: AssemblyFileVersionAttribute("0.3")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3"
