using Autofac;
using Autofac.Integration.Mvc;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Todolist.Models;
using Todolist.Services;
using Todolist.Services.Contracts;

namespace Todolist
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.RegisterType(typeof(DbRepository)).As(typeof(IDbRepository)).InstancePerRequest();
            builder.RegisterType(typeof(RequestProcessor)).As(typeof(IRequestProcessor)).InstancePerRequest();
            builder.RegisterType(typeof(HistoricalDataGrabber)).As(typeof(IHistoricalDataGrabber)).InstancePerRequest();
            builder.RegisterType(typeof(SignalGenerator)).As(typeof(ISignalGenerator)).InstancePerRequest();
            builder.RegisterType<ToDoListDbEntities>().AsSelf().InstancePerRequest();

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}
