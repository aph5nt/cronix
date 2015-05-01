﻿namespace Cronix.Web

module Website =
   
    open System
    open Nancy.Conventions
    open Nancy.TinyIoc
    open Nancy.Bootstrapper
    open Nancy
 
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
    
        type WebUiModule() as self = 
            inherit NancyModule()
            do
                self.Get.["/"] <- fun _ -> self.Index()

            member self.Index() =
                base.View.["index.html"] :> obj