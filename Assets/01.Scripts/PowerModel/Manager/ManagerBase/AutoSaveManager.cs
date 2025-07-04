using System.Linq;
using Akasha;
using UnityEngine;


public class AutoSaveManager : ManagerBase
{
    public override int InitializationPriority => 55;

    [Header("Auto Save Settings")]
    [SerializeField] private bool enableAutoSave = true;
    [SerializeField] private float autoSaveInterval = 60f;
    [SerializeField] private bool saveOnApplicationPause = true;
    [SerializeField] private bool saveOnApplicationFocus = true;

    private float lastSaveTime;
    private TimerHandle autoSaveTimer;

    protected override void OnManagerAwake()
    {
        base.OnManagerAwake();

        if (enableAutoSave)
        {
            autoSaveTimer = UnityTimer.ScheduleRepeating(autoSaveInterval, PerformAutoSave);
        }
    }

    protected override void OnManagerDestroy()
    {
        if (autoSaveTimer != null)
        {
            autoSaveTimer.Cancel();
        }

        base.OnManagerDestroy();
    }

    public override void OnApplicationPauseChanged(bool pauseStatus)
    {
        if (saveOnApplicationPause && pauseStatus)
        {
            PerformAutoSave();
        }
    }

    public override void OnApplicationFocusChanged(bool hasFocus)
    {
        if (saveOnApplicationFocus && !hasFocus)
        {
            PerformAutoSave();
        }
    }

    private void PerformAutoSave()
    {
        if (Time.time - lastSaveTime < 1f) return;

        var dirtyControllers = GameManager.ModelControllers.GetAll()
            .Where(c => c.isDirty)
            .ToList();

        if (dirtyControllers.Count > 0)
        {
            if (GameManager.SaveLoad.SaveGame())
            {
                lastSaveTime = Time.time;
                Debug.Log($"[AutoSaveManager] Auto-saved {dirtyControllers.Count} dirty models");

                foreach (var controller in dirtyControllers)
                {
                    controller.isDirty = false;
                }
            }
        }
    }
}