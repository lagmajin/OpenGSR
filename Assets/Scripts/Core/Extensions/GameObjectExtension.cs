using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenGS
{
    public static class GameObjectExtension
    {
        public static bool HasChild(this GameObject gameObject)
        {
            return 0 < gameObject.transform.childCount;
        }

        public static void SetLayer(this GameObject gameObject, int layer, bool needSetChildrens = true)
        {
            if (!gameObject) return;

            gameObject.layer = layer;

            if (!needSetChildrens) return;

            foreach (Transform childTransform in gameObject.transform)
                SetLayer(childTransform.gameObject, layer, needSetChildrens);
        }

        public static void SetLayer(this GameObject gameObject, string layerName, bool needSetChildrens = true)
        {
            SetLayer(gameObject, LayerMask.NameToLayer(layerName), needSetChildrens);
        }

        public static void SetInvert(this GameObject self)
        {
            bool inv = !self.activeSelf;
            self.SetActive(inv);
        }

        public static Scene GetBelongsScene(this GameObject target)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid())
                {
                    continue;
                }

                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    if (root == target.transform.root.gameObject)
                    {
                        return scene;
                    }
                }
            }

            return default(Scene);
        }

        public static string GetHierarchyPath(this GameObject self)
        {
            string path = "";
            Transform current = self.transform;
            while (current != null)
            {
                // 同じ階層に同名のオブジェクトがある場合があるので、それを回避する
                int index = current.GetSiblingIndex();
                path = "/" + current.name + index + path;
                current = current.parent;
            }
            Scene belongScene = self.GetBelongsScene();

            return "/" + belongScene.name + path;
        }

        public static T FindObjectOfInterface<T>() where T : class
        {
            foreach (var n in GameObject.FindObjectsByType<Component>(FindObjectsSortMode.None))
            {
                var component = n as T;
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }
    }
}
