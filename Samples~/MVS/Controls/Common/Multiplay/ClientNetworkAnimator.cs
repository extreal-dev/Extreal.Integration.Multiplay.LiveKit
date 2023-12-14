using Unity.Netcode.Components;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.Common.Multiplay
{
    public class ClientNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
