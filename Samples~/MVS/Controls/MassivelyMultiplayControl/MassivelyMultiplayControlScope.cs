using Extreal.Integration.Multiplay.Common.MVS.Controls.MassivelyMultiplyControl.Client;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.MassivelyMultiplyControl
{
    public class MassivelyMultiplayControlScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
            => builder.RegisterEntryPoint<MassivelyMultiplayClientPresenter>();
    }
}
