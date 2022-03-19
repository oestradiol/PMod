using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PMod.Modules;

internal class ModsAllower : ModuleBase
{
    public ModsAllower() : base(true)
    {
        useOnSceneWasLoaded = true;
        RegisterSubscriptions();
    }

    protected override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        var activeScene = SceneManager.GetActiveScene();
        foreach (var rootGameObject in activeScene.GetRootGameObjects())
            if (rootGameObject.name is "eVRCRiskFuncDisable" or "UniversalRiskyFuncDisable" || rootGameObject.name.Contains("CTBlockAction_"))
                Object.DestroyImmediate(rootGameObject);
        SceneManager.MoveGameObjectToScene(new GameObject("eVRCRiskFuncEnable"), activeScene);
        SceneManager.MoveGameObjectToScene(new GameObject("UniversalRiskyFuncEnable"), activeScene);
    }
}