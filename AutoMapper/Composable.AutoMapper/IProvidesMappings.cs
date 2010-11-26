using AutoMapper;

namespace Composable.AutoMapper
{
    public interface IProvidesMappings
    {
        void CreateMappings(SafeConfiguration configuration);
    }
}