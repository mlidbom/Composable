using System;

namespace Composable.Persistence.EventStore
{
    interface IEventTypeToIdMapper
    {
        Type GetType(int id);
        int GetId(Type type);
        void LoadTypesFromDatabase();
    }
}