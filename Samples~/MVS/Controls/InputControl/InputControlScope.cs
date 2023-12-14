using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.InputControl
{
    public class InputControlScope : LifetimeScope
    {
        [SerializeField] private InputControlView inputControlView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(inputControlView);

            builder.RegisterEntryPoint<InputControlPresenter>();
        }
    }
}
