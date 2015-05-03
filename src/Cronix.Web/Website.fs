namespace Cronix.Web

/// Module responsible for nancy settings.
module Website =
   
    open System
    open Nancy.Conventions
    open Nancy.TinyIoc
    open Nancy.Bootstrapper
    open Nancy
 
     /// Nancy web boostrapper
     type WebBootstrapper() =
        inherit DefaultNancyBootstrapper()
        
        override x.ApplicationStartup( container : TinyIoCContainer,  pipelines : IPipelines) =
            let impl viewName model content = 
                String.Concat("webui/", viewName)
            let convestion() = 
                Func<string, obj, ViewEngines.ViewLocationContext, string>(impl)
            base.Conventions.ViewLocationConventions.Add( convestion() )
             
        override x.ConfigureConventions(conventions : NancyConventions) =
             conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/", "webui"))
             base.ConfigureConventions(conventions)
    
    /// WebUi nancy module
    type WebUiModule() as self = 
        inherit NancyModule()
        do
            self.Get.["/"] <- fun _ -> self.Index()

        member self.Index() =
            base.View.["index.html"] :> obj