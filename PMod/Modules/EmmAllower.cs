using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PMod.Modules
{
    internal class EmmAllower : ModuleBase
    {
        #region ComponentToggle Shit
        internal static readonly string Base = "CTBlockAction_";
        internal static readonly string[] names = { $"{Base}1", $"{Base}2", $"{Base}3", $"{Base}4", $"{Base}5", $"{Base}6", $"{Base}7" };
        #endregion
        #region TeleporterVR Shit (And future emmVRC)
        internal static readonly string UniEnable = "UniversalRiskyFuncEnable", UniDisable = "UniversalRiskyFuncDisable";
        #endregion

        internal override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject rootGameObject in activeScene.GetRootGameObjects())
                if (rootGameObject.name == "eVRCRiskFuncDisable" || rootGameObject.name == UniDisable ||
                    rootGameObject.name == Base + names[0] || rootGameObject.name == Base + names[1] || rootGameObject.name == Base + names[2] ||
                    rootGameObject.name == Base + names[3] || rootGameObject.name == Base + names[4] || rootGameObject.name == Base + names[5] || rootGameObject.name == Base + names[6])
                    Object.DestroyImmediate(rootGameObject);
            SceneManager.MoveGameObjectToScene(new GameObject("eVRCRiskFuncEnable"), activeScene);
            SceneManager.MoveGameObjectToScene(new GameObject(UniEnable), activeScene);
        }
    }
}