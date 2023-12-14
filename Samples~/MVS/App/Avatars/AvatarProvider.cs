using UnityEngine;

namespace Extreal.Integration.Multiplay.Common.MVS.App.Avatars
{
    public class AvatarProvider : MonoBehaviour
    {
        [SerializeField] private Avatar avatar;

        public Avatar Avatar => avatar;
    }
}
