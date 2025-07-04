using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : ManagerBase
{
    public override int InitializationPriority => 45;

    [SerializeField] private bool saveBeforeSceneChange = true;

    public override void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        if (saveBeforeSceneChange && previousScene.isLoaded)
        {
            GameManager.SaveLoad?.SaveGame();
        }
    }
}
