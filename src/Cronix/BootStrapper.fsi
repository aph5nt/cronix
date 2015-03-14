namespace Cronix

    open Chessie.ErrorHandling

    module BootStrapper = 
        val InitService : Option<string[]> * Option<StartupHandler> -> Result<string, string>
