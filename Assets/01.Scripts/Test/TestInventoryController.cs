using UnityEngine;
using Akasha;

public class TestInventoryController : MController<InventoryModel>
{
    protected override bool EnableSaveLoad => true;

    protected override void CreateModel()
    {
        Model = new InventoryModel();
        Debug.Log("[TestInventoryController] Inventory model created");
    }

    protected override void SetModel()
    {
        Model.Gold.AddListener(gold => {
            Debug.Log($"[Inventory] Gold changed: {gold}");
            MarkDirty();
        });

        Model.ItemAdded += (item) => {
            Debug.Log($"[Inventory] Item added: {item.Name} x{item.Quantity}");
            MarkDirty();
        };

        Model.ItemRemoved += (itemId) => {
            Debug.Log($"[Inventory] Item removed: ID {itemId}");
            MarkDirty();
        };
    }

    protected override void AtInit()
    {
        Debug.Log("[TestInventoryController] Init - Inventory system initialized");
    }

    protected override void AtLoad()
    {
        Debug.Log("[TestInventoryController] Load - Loading inventory data");
        // SaveLoadManager가 자동으로 처리하므로 추가 작업 필요 없음
    }

    protected override void AtReadyModel()
    {
        Debug.Log("[TestInventoryController] ReadyModel - Inventory ready");
        PrintInventory();
    }

    protected override void AtSave()
    {
        Debug.Log("[TestInventoryController] Save - Saving inventory data");
        // SaveLoadManager가 자동으로 처리하므로 추가 작업 필요 없음
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddTestItem("Health Potion", 5);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AddTestItem("Mana Potion", 3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Model.AddGold(100);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Model.SpendGold(50);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            PrintInventory();
        }
    }

    private void AddTestItem(string name, int quantity)
    {
        var item = new ItemData
        {
            Id = name.GetHashCode(),
            Name = name,
            Quantity = quantity,
            MaxStack = 99
        };

        Model.AddItem(item);
    }

    private void PrintInventory()
    {
        Debug.Log($"=== INVENTORY STATUS ===");
        Debug.Log($"Gold: {Model.Gold.Value}");
        Debug.Log($"Items: {Model.ItemCount}");

        foreach (var item in Model.GetAllItems())
        {
            Debug.Log($"- {item.Name} x{item.Quantity}");
        }
        Debug.Log($"=====================");
    }
}