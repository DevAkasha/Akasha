using System.Collections;
using System.Collections.Generic;
using Akasha;
using UnityEngine;

public class TestUIPresenter : BasePresenter
{
    private TestUIModel model;
    private TimerHandle updateTimer;

    protected override void AtAwake()
    {
        Debug.Log($"[{GetAggregateId()}] AtAwake - UI Presenter");
        model = new TestUIModel();
    }

    protected override void AtInit()
    {
        Debug.Log($"[{GetAggregateId()}] AtInit - UI Presenter initialized");

        model.score.AddListener(OnScoreChanged);
        model.playerName.AddListener(OnPlayerNameChanged);
        model.gameStarted.AddListener(OnGameStateChanged);

        updateTimer = this.RepeatingCall(2.0f, UpdateScore);
    }

    protected override void AtDeinit()
    {
        Debug.Log($"[{GetAggregateId()}] AtDeinit - UI Presenter cleanup");
        updateTimer?.Cancel();
    }

    private void OnScoreChanged(int score)
    {
        Debug.Log($"[{GetAggregateId()}] Score updated: {score}");
    }

    private void OnPlayerNameChanged(string name)
    {
        Debug.Log($"[{GetAggregateId()}] Player name changed: {name}");
    }

    private void OnGameStateChanged(bool started)
    {
        Debug.Log($"[{GetAggregateId()}] Game state: {(started ? "Started" : "Stopped")}");
    }

    private void UpdateScore()
    {
        if (model.gameStarted.Value)
        {
            model.score.Set(model.score.Value + 100);
        }
    }

    public void StartGame()
    {
        model.gameStarted.Set(true);
        Debug.Log($"[{GetAggregateId()}] Game started!");
    }

    public void StopGame()
    {
        model.gameStarted.Set(false);
        Debug.Log($"[{GetAggregateId()}] Game stopped!");
    }

    public void SetPlayerName(string name)
    {
        model.playerName.Set(name);
    }
}