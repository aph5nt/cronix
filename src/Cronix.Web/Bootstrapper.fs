namespace Cronix.Web

open Nancy
open Nancy.TinyIoc
open Nancy.Bootstrapper
open Nancy.Conventions

type Bootstrapper() =
    inherit DefaultNancyBootstrapper()

    override x.ApplicationStartup(container :TinyIoCContainer,  pipelines : IPipelines) =
        base.ApplicationStartup(container, pipelines)
   
    override x.ConfigureConventions(nancyConventions : NancyConventions) =
        nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("Scripts", @"/Scripts"))
        base.ConfigureConventions(nancyConventions)
  