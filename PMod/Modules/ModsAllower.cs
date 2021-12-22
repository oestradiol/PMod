using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PMod.Modules
{
    internal class ModsAllower : ModuleBase
    {
        internal ModsAllower()
        {
            useOnSceneWasLoaded = true;
            RegisterSubscriptions();
        }

        internal override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject rootGameObject in activeScene.GetRootGameObjects())
                if (rootGameObject.name == "eVRCRiskFuncDisable" || rootGameObject.name == "UniversalRiskyFuncDisable" || rootGameObject.name.Contains("CTBlockAction_"))
                    Object.DestroyImmediate(rootGameObject);
            SceneManager.MoveGameObjectToScene(new GameObject("eVRCRiskFuncEnable"), activeScene);
            SceneManager.MoveGameObjectToScene(new GameObject("UniversalRiskyFuncEnable"), activeScene);
        }
    }
}