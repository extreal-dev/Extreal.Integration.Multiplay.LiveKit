using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.Spaces.VirtualSpace
{
    public class VirtualSpaceScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
            => builder.RegisterEntryPoint<VirtualSpacePresenter>();
    }
}
