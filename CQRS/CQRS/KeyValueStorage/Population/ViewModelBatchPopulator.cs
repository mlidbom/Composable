using Castle.Windsor;
using System;
using System.Threading.Tasks;
using Composable.System.Linq;
using JetBrains.Annotations;

namespace Composable.KeyValueStorage.Population
{
    [UsedImplicitly]
    public class ViewModelBatchPopulator : IViewModelBatchPopulator
    {

        private readonly IWindsorContainer _container;
        private readonly IPopulationBatchLogger _logger;

        public ViewModelBatchPopulator(IWindsorContainer container, IPopulationBatchLogger logger)
        {
            _container = container;
            _logger = logger;
        }       

        public void PopulateEntities(params Guid[] aggregateRootIds)
        {
           _logger.Initialize(aggregateRootIds.Length);

            Parallel.ForEach(
                aggregateRootIds,
                entityId =>
                {
                    try
                    {
                        using(var unitOfWork = _container.BeginTransactionalUnitOfWorkScope())
                        {
                            _container.ResolveAll<IViewModelPopulator>()
                                      .ForEach(populator => populator.Populate(entityId));
                            
                            unitOfWork.Commit();                            
                        }
                        _logger.LogAggregateHandled(entityId);
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e, entityId);
                    }
                });
        }      

    }
}
