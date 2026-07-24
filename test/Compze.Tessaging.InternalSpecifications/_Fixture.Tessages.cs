using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;
using Compze.TypeIdentifiers.DependencyInjection;

[assembly: AssemblyTypeMapper(typeof(Compze.Tessaging.InternalSpecifications.AssemblyTypeMapper))]

// ReSharper disable ClassNeverInstantiated.Global
namespace Compze.Tessaging.InternalSpecifications;

public class TueryAnswer
{
   public string Message { get; set; } = "";
}

public class TueryOnlyADownPeerServes : Remotable.NonTransactional.Tueries.Tuery<TueryAnswer>;

///<summary>A best-effort tevent that records which publish it was, so a specification can assert exactly which of a series of
/// publishes reached a subscriber — and, just as importantly, which did not.</summary>
public interface ISequencedBestEffortTevent : IRemotableTevent
{
   int SequenceNumber { get; }
}

public class SequencedBestEffortTevent : ISequencedBestEffortTevent
{
   public int SequenceNumber { get; set; }
}

#pragma warning disable CA1812 // Instantiated via reflection through the [assembly: AssemblyTypeMapper(typeof(...))] attribute.
class AssemblyTypeMapper : IAssemblyTypeMapper
{
   public void Map(IAssemblyTypeMappingRegistrar map)
      => map.Map<TueryOnlyADownPeerServes>("4b0e7d92-5c1a-4e83-9f27-6d38a0b45c19")
            .Map<MyExactlyOnceTommandAdmittedBeforeTheCrash>("7c2f4a81-9d36-4b58-8e07-1f65c3a92d40")
            .Map<MyExactlyOnceTommandWhoseAdmissionIsTheOpenTransactionsWrite>("2fafd23c-e4cf-4fa4-8d2a-0b7917ca090c")
            .Map<ISequencedBestEffortTevent>("9f31c0a7-6b2e-4854-a1d9-38e7052bc46f")
            .Map<SequencedBestEffortTevent>("2e8b56c3-1a94-4d07-b3f8-5c60d729ae13");
}

static class TessagingInternalSpecificationTypeMappings
{
   extension(IComponentRegistrar @this)
   {
      ///<summary>Requires the type identity of this internal-specification assembly — its test message types.</summary>
      public IComponentRegistrar RequireTessagingInternalSpecificationTypeMappings()
         => @this.RequireMappedTypesFromAssemblyContaining<AssemblyTypeMapper>();
   }
}
