namespace Cronix.Web

open Nancy
open Nancy.TinyIoc
open Nancy.Bootstrapper

type Bootstrapper() =
    inherit DefaultNancyBootstrapper()

    override x.ApplicationStartup(container :TinyIoCContainer,  pipelines : IPipelines) =
        ()
   
