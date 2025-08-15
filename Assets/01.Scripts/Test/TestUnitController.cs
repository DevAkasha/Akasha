using System.Collections;
using System.Collections.Generic;
using Akasha.Modifier;
using Akasha;
using UnityEngine;

public class TestUnitController : MController<TestUnitModel>
{
    [Header("Test Unit Settings")]
    public float damageAmount = 10f;
    public float healAmount = 5f;

    protected override void AtAwake()
    {
        Debug.Log($"[{GetAggregateId()}] AtAwake - Unit Controller");
    }

    protected override TestUnitModel SetModel()
    {
        return new TestUnitModel($"Unit_{GetInstanceID()}");
    }

    protected override void AtModelReady()
    {
        Debug.Log($"[{GetAggregateId()}] Model Ready - Unit: {Model.unitName.Value}");

        Model.health.AddListener(OnHealthChanged);
        Model.speed.AddListener(OnSpeedChanged);
        Model.isActive.AddListener(OnActiveChanged);
    }

    protected override void AtInit()
    {
        Debug.Log($"[{GetAggregateId()}] AtInit - Unit initialized with health: {Model.health.Value}");
    }

    private void OnHealthChanged(int newHealth)
    {
        Debug.Log($"[{GetAggregateId()}] Health changed: {newHealth}");
        if (newHealth <= 0)
        {
            Model.isActive.Set(false);
            Debug.Log($"[{GetAggregateId()}] Unit died!");
        }
    }

    private void OnSpeedChanged(float newSpeed)
    {
        Debug.Log($"[{GetAggregateId()}] Speed changed: {newSpeed}");
    }

    private void OnActiveChanged(bool active)
    {
        Debug.Log($"[{GetAggregateId()}] Active state: {active}");
        gameObject.SetActive(active);
    }

    public void TakeDamage()
    {
        var currentHealth = Model.health.Value;
        Model.health.Set(currentHealth - (int)damageAmount);
        Debug.Log($"[{GetAggregateId()}] Took {damageAmount} damage. Health: {Model.health.Value}");
    }

    public void Heal()
    {
        var currentHealth = Model.health.Value;
        Model.health.Set(currentHealth + (int)healAmount);
        Debug.Log($"[{GetAggregateId()}] Healed {healAmount}. Health: {Model.health.Value}");
    }

    public void AddHealthModifier(string modifierName, float value)
    {
        var key = ModifierKey.Create(modifierName);
        Model.health.SetModifier(key, ModifierType.OriginAdd, value);
        Debug.Log($"[{GetAggregateId()}] Added health modifier '{modifierName}': {value}");
    }

    public void RemoveHealthModifier(string modifierName)
    {
        var key = ModifierKey.Create(modifierName);
        Model.health.RemoveModifier(key);
        Debug.Log($"[{GetAggregateId()}] Removed health modifier '{modifierName}'");
    }

    protected override void AtPoolInit()
    {
        Debug.Log($"[{GetAggregateId()}] AtPoolInit - Unit reactivated");
        Model.isActive.Set(true);
    }

    protected override void AtSave()
    {
        Debug.Log($"[{GetAggregateId()}] AtSave - Saving unit data");
    }
}
