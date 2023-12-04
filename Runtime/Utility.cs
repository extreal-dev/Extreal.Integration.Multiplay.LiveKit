using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Extreal.Integration.Multiplay.LiveKit
{
    public static class Utility
    {
        public static void DebugLogger(object obj, object message)
        {
            if (obj == null)
            {
                Debug.Log($"{message}");
            }
            else
            {
                Debug.Log($"[{obj.GetType().Name}] - {message}");
            }
        }

        /// <summary>
        /// ヒエラルキーに応じたパスを取得する
        // https://edom18.hateblo.jp/entry/2018/01/05/135020
        // string id = GameObjectUtility.GetHierarchyPath(gameObject);
        // int hash = id.GetHashCode();
        /// </summary>
        public static string GetHierarchyPath(GameObject target)
        {
            string path = "";
            Transform current = target.transform;

            while (current != null)
            {
                // 同じ階層に同名のオブジェクトがあるときの回避処理
                int index = current.GetSiblingIndex();
                path = "/" + current.name + index + path;
                current = current.parent;
            }
            Scene belongScene = target.GetBelongScene();
            return "/" + belongScene.name + path;
        }

        public static int GetGameObjectHash(GameObject target)
        {
            string id = GetHierarchyPath(target);
            return id.GetHashCode();
        }
    }

    //
    public static class GameObjectExtension
    {
        public static Scene GetBelongScene(this GameObject target)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid())
                {
                    continue;
                }
                if (!scene.isLoaded)
                {
                    continue;
                }
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                {
                    if (root == target.transform.root.gameObject)
                    {
                        return scene;
                    }
                }
            }
            return default(Scene);
        }
    }

    public static class ObjectExtensions
    {
        public static void Destroy(this Object self)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying == false)
            {
                GameObject.DestroyImmediate(self);
            }
            else
#endif
            {
                GameObject.Destroy(self);
            }
        }
    }
}
