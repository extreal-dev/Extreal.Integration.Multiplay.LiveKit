using System.Collections.Generic;
using UnityEngine;

namespace Extreal.Integration.Multiplay.Common
{
    public interface INetworkObjectsProvider
    {
        List<GameObject> Provide();
    }
}
