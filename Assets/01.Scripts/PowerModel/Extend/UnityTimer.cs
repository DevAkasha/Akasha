
using System;
using System.Collections.Generic;
using UnityEngine;

// 유니티 타이머 유틸 (MonoBehaviour 없이 처리 가능하도록 별도 구현)
public static class UnityTimer
{
    private static readonly List<TimerTask> tasks = new();

    public static TimerHandle ScheduleRepeating(float interval, Action callback)
    {
        var task = new TimerTask(interval, callback, true);
        tasks.Add(task);
        return new TimerHandle(task);
    }

    public static TimerHandle ScheduleOnce(float delay, Action callback)
    {
        var task = new TimerTask(delay, callback, false);
        tasks.Add(task);
        return new TimerHandle(task);
    }

    public static void Cancel(TimerHandle handle)
    {
        if (handle != null && handle.Task != null)
        {
            tasks.Remove(handle.Task);
            handle.Invalidate();
        }
    }

    public static void CancelAll()
    {
        tasks.Clear();
    }

    public static void Tick(float deltaTime)
    {
        // 완료된 타이머들을 저장할 리스트
        var completedTasks = new List<TimerTask>();

        // 역순으로 순회하여 안전하게 제거
        for (int i = tasks.Count - 1; i >= 0; i--)
        {
            var task = tasks[i];
            if (task.Update(deltaTime))
            {
                // 일회성 타이머가 완료되면 제거
                if (!task.IsRepeating)
                {
                    tasks.RemoveAt(i);
                }
            }
        }
    }

    public static int ActiveTimerCount => tasks.Count;
}

public class TimerTask
{
    private readonly float interval;
    private readonly Action callback;
    private float time;

    public bool IsRepeating { get; private set; }
    public bool IsValid { get; private set; } = true;

    public TimerTask(float interval, Action callback, bool isRepeating)
    {
        this.interval = interval;
        this.callback = callback;
        this.IsRepeating = isRepeating;
    }

    public bool Update(float delta)
    {
        if (!IsValid) return false;

        time += delta;
        if (time >= interval)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityTimer] Timer callback error: {ex.Message}");
            }

            if (IsRepeating)
            {
                time = 0f; // 반복 타이머는 시간 리셋
            }
            else
            {
                IsValid = false; // 일회성 타이머는 무효화
            }

            return true;
        }

        return false;
    }

    public void Invalidate()
    {
        IsValid = false;
    }
}
public class TimerHandle
{
    public TimerTask Task { get; private set; }

    public TimerHandle(TimerTask task)
    {
        Task = task;
    }

    public bool IsValid => Task?.IsValid ?? false;

    public void Cancel()
    {
        UnityTimer.Cancel(this);
    }

    internal void Invalidate()
    {
        Task = null;
    }
}
public static class UnityTimerExtensions
{
    public static TimerHandle DelayedCall(this MonoBehaviour mono, float delay, Action callback)
    {
        return UnityTimer.ScheduleOnce(delay, callback);
    }

    public static TimerHandle RepeatingCall(this MonoBehaviour mono, float interval, Action callback)
    {
        return UnityTimer.ScheduleRepeating(interval, callback);
    }
}