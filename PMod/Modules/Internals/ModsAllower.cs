using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PMod.Modules.Internals;

internal class ModsAllower : VrcMod
{
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var rootGameObject in activeScene.GetRootGameObjects())
            if (rootGameObject.name is "eVRCRiskFuncDisable" or "UniversalRiskyFuncDisable" || rootGameObject.name.Contains("CTBlockAction_"))
                Object.DestroyImmediate(rootGameObject);
        SceneManager.MoveGameObjectToScene(new GameObject("eVRCRiskFuncEnable"), activeScene);
        SceneManager.MoveGameObjectToScene(new GameObject("UniversalRiskyFuncEnable"), activeScene);
    }
}