using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.Screens.LoadingScreen
{
    public class LoadingScreenScope : LifetimeScope
    {
        [SerializeField] private LoadingScreenView loadingScreenView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(loadingScreenView);

            builder.RegisterEntryPoint<LoadingScreenPresenter>();
        }
    }
}
