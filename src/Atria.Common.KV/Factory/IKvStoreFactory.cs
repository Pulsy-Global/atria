using Atria.Common.KV.Interfaces;

namespace Atria.Common.KV.Factory;

public interface IKvStoreFactory
{
    Task<IKvStore> CreateAsync(string namespaceName);
}
