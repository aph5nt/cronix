namespace Cronix

open System
open Logging

/// Module responsible for compiling the startup.fsx script.
[<AutoOpen>]
module Compiler =
    
    open System.Reflection
    open System.IO
    open Chessie.ErrorHandling
    open FSharp.Compiler.CodeDom
    open System.CodeDom.Compiler

    /// Statup script name constant.
    let startupFile = "Startup.fsx"

    /// Output assembly name constant.
    let outputAssembly = "Cronix.Startup.dll"

    /// Compiler module specific logger.
    let private logger = logger()

    /// Scans for assemblies in the cronix service directory. Returns assembly name list.
    let loadAssemblies() =
        let assemblies =
            Directory.GetFiles(".", "*.dll", SearchOption.TopDirectoryOnly)
            |> Seq.map(fun(f) -> Path.GetFileName(f)) 
            |> Seq.filter ((<>)outputAssembly)
            |> Seq.sort
            |> Seq.toArray 
        let defaultAssemblies  = 
            let entry = Assembly.GetEntryAssembly()
            let executing = Assembly.GetExecutingAssembly()
            let calling = Assembly.GetCallingAssembly()
            [|"System.dll"; 
                (if entry <> null then Path.GetFileName(entry.Location) else "");
                (if executing <> null then Path.GetFileName(executing.Location) else "");
                (if calling <> null then Path.GetFileName(calling.Location) else "");
            |] 
            |> Seq.distinct |> Seq.toArray
        let asm = Array.append assemblies defaultAssemblies 
                 |> Array.toSeq
                 |> Seq.filter ((<>)"")
                 |> Seq.distinct
                 |> Seq.toArray

        ok asm

     /// Loads the startup script.
    let getStartupScript() =
        try
            let sourceCode = File.ReadAllText("Startup.fsx")
            ok sourceCode
        with
        | exn -> fail(sprintf "Failed to load source code from the starup script. %s" exn.Message)

    /// Compiles the startup script.
    let compileScript(state : StartupScriptState) =
        try
            let provider = new FSharpCodeProvider()
            let params'= CompilerParameters()
            params'.GenerateExecutable <- false
            params'.OutputAssembly <- IO.Path.Combine(System.Environment.CurrentDirectory, outputAssembly)
            params'.IncludeDebugInformation <- true
            params'.ReferencedAssemblies.AddRange(state.referencedAssemblies)
            params'.GenerateInMemory <- false
            params'.WarningLevel <- 3
            params'.TreatWarningsAsErrors <- false

            let compiledResults = provider.CompileAssemblyFromSource(params', state.source)

            if compiledResults.Errors.Count > 0 then
                let errors = Array.init<CompilerError> compiledResults.Errors.Count (fun(_)-> new CompilerError())
                compiledResults.Errors.CopyTo(errors, 0)
                let errorMessage = seq { for error in errors do yield error.ErrorText} |> Seq.toArray   
                let errorMessages = Array.append [|"Failed to compile startup script."|] errorMessage
                fail(String.Join("\n", errorMessages))
            else
                let asm = Some(compiledResults.CompiledAssembly)
                ok { state with compiled = asm}
        with
        | exn -> fail(sprintf "Failed to compile startup script. %s" exn.Message)