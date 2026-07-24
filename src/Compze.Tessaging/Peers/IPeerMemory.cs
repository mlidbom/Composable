using Compze.Tessaging.Peers._internal;

namespace Compze.Tessaging.Peers;

///<summary>The endpoint's remembered peers, read-only: every peer whose advertisement the endpoint's memory holds, connected<br/>
/// or not (see <c>src/Compze.Tessaging/dev_docs/peers.md</c>). The wait predicate for "has this endpoint met that peer yet",<br/>
/// and the read side any future peer-administration surface will build on<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/WIP/peer-administration.md</c>). Every transport-speaking endpoint registers one,<br/>
/// alongside its <see cref="IPeerRegistry"/>.</summary>
public interface IPeerMemory
{
   ///<summary>The endpoint's remembered peers — each peer's identity and its last-known advertisement.</summary>
   IReadOnlyList<RememberedPeer> Peers { get; }
}
