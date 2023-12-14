using Extreal.Core.StageNavigation;
using Extreal.Integration.Multiplay.Common.MVS.App;
using Extreal.Integration.Multiplay.Common.MVS.App.Config;
using Extreal.Integration.Multiplay.Common.MVS.Spaces.Common;

namespace Extreal.Integration.Multiplay.Common.MVS.Spaces.VirtualSpace
{
    public class VirtualSpacePresenter : SpacePresenterBase
    {
        public VirtualSpacePresenter
        (
            StageNavigator<StageName, SceneName> stageNavigator,
            AppState appState
        ) : base(stageNavigator, appState)
        {
        }
    }
}
