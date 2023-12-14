using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Extreal.Integration.Multiplay.Common.MVS.Controls.SpaceControl
{
    public class SpaceControlView : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text backButtonLabel;

        public IObservable<Unit> OnBackButtonClicked
            => backButton.OnClickAsObservable().TakeUntilDestroy(this);

        private void Awake()
        {
            backButtonLabel.text = "Back";
        }
    }
}
