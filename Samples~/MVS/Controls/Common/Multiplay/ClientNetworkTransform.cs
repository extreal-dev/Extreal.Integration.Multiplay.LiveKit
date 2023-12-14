using Unity.Netcode.Components;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
