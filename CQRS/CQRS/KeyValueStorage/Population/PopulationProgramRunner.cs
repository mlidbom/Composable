using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Windsor;
using Castle.MicroKernel.Lifestyle;

namespace Composable.KeyValueStorage.Population
{
    public class PopulationProgramRunner
    {
        private readonly IWindsorContainer _container;
        private static IViewModelBatchPopulator _batchPopulator;

        public PopulationProgramRunner(IWindsorContainer container)
        {
            _container = container;
        }

        public void Run(string[] args)
        {
            Guid[] ids;
            using(_container.BeginScope())
            {
                var idFetcher = _container.Resolve<AggregateIdFetcher>();
                ids = new Guid[] {};

                if(args.Count() != 1)
                {
                    Usage();
                    return;
                }

                if(args.First().ToLower().Equals("-all"))
                {
                    ids = idFetcher.GetAll();
                }
                else if(args.First().ToLower().StartsWith("-entities:"))
                {
                    IEnumerable<Guid> entityIds;
                    try
                    {
                        entityIds = ExtractGuids(args.First());
                    }
                    catch(Exception e)
                    {
                        Usage();
                        return;
                    }

                    ids = entityIds.ToArray();

                }
                else if(args.First().ToLower().StartsWith("-allafter:"))
                {
                    Guid eventId;
                    if(Guid.TryParse(ExtractSwitchArgument(args.First()), out eventId))
                        ids = idFetcher.GetEntitiesCreatedAfter(eventId);
                    else
                        Usage();
                }
                else if (args.First().ToLower().StartsWith("-fromfile:"))
                {
                    string filePath = ExtractSwitchArgument(args.First()).Trim();
                    if(!File.Exists(filePath))
                    {
                        Console.WriteLine("Could not find file:{0}", filePath);
                        return;
                    }
                    ids = idFetcher.GetEntitiesFromFile(File.OpenRead(filePath));
                }
                else
                    Usage();
            }

            _container.Resolve<IViewModelBatchPopulator>().PopulateEntities(ids);
        }

        private IEnumerable<Guid> ExtractGuids(string ids)
        {
            return ExtractSwitchArgument(ids).Split(',').Select(Guid.Parse).ToList();
        }

        private static string ExtractSwitchArgument(string argument)
        {
            var splittChar = new[] { ':' };
            var argArr = argument.Split(splittChar, StringSplitOptions.RemoveEmptyEntries);
            return argArr.Count() == 2 ? argArr[1] : "";
        }

        private static void Usage()
        {
            Console.WriteLine(@"Usage: ");
            Console.WriteLine(@"
-All                   --> Repopulates all viewmodels for all entities
-Entities:{entityid},{entityid}.... --> Repopulates ViewModel for selected entities
-AllAfter:{eventid}    --> Repopulate all ViewModels after selected event
-FromFile:{filepath} --> Repopulate all viewmodels from the file with an aggregateid per row.
            ");
        }
    }
}
