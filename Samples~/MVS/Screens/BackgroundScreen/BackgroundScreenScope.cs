using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.BackgroundScreen
{
    public class BackgroundScreenScope : LifetimeScope
    {
        [SerializeField] private BackgroundScreenView backgroundScreenView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(backgroundScreenView);

            builder.RegisterEntryPoint<BackgroundScreenPresenter>();
        }
    }
}
