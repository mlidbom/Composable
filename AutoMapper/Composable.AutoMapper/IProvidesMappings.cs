using System;

namespace Composable.AutoMapper
{
    [Obsolete("Will be removed soon. Migrate away immediately!")]
    public interface IProvidesMappings
    {
        void CreateMappings(SafeConfiguration configuration);
    }
}