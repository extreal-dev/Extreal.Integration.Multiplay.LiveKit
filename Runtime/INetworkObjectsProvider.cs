using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    /// <summary>
    /// Interface for providing MultiplayClient with GameObjects to be synchronized over the network.
    /// </summary>
    public interface INetworkObjectsProvider
    {
        /// <summary>
        /// provides GameObjects to be synchronized over the network.
        /// </summary>
        /// <returns>GameObjects to be synchronized over the network.</returns>
        List<GameObject> Provide();
    }
}
