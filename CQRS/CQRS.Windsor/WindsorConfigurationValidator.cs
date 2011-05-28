#region usings

using System;
using System.Linq;
using System.Text;
using Castle.Core;
using Castle.MicroKernel;
using Castle.Windsor;

#endregion

namespace Composable.CQRS.Windsor
{
    public static class WindsorConfigurationValidator
    {
        public static void AssertConfigurationValid(this IWindsorContainer container)
        {
            var assignableHandlers = container.Kernel.GetAssignableHandlers(typeof(object)).ToArray();
            var faulty = assignableHandlers
                .Where(handler => handler.CurrentState == HandlerState.WaitingDependency)
                .ToArray();

            if(!faulty.Any())
            {
                return;
            }

            var message = new StringBuilder();
            message.AppendLine("\nThe following registered types are awaiting dependencies:");
            foreach(var handler in faulty)
            {
                message.AppendFormat("\t" + handler.ComponentModel.Name + "\n");
                message.AppendLine("\t\tMissing dependencies:");                
                handler.ComponentModel.Constructors
                    .SelectMany(constructor => constructor.Dependencies)
                    .Select(model => model.TargetItemType)
                    .Where(type => !container.Kernel.GetAssignableHandlers(type).Any())
                    .Distinct()
                    .ForEach(comp => message.AppendLine("\t\t\t" + comp));
                message.AppendLine();
            }

            message.AppendLine("\n\tAll Missing dependencies:");

            faulty.SelectMany(broken => broken.ComponentModel.Constructors)
                .SelectMany(constructor => constructor.Dependencies)
                .Select(model => model.TargetItemType)
                .Where(type => !container.Kernel.GetAssignableHandlers(type).Any())
                .Distinct()
                .ForEach(comp => message.AppendLine("\t\t" + comp));

            throw new Exception(message.ToString());
        }
    }
}