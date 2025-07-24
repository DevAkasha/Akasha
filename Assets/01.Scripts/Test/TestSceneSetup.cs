using UnityEngine;
using UnityEngine.UI;
using Akasha;
using Akasha.Modifier;

public class TestSceneSetup : MonoBehaviour
{
    void Start()
    {
        SetupGameManager();
        SetupPlayer();
        SetupUI();
        SetupTestEnvironment();

        Debug.Log("=== Test Scene Setup Complete ===");
        PrintInstructions();
    }

    private void SetupGameManager()
    {
        if (GameManager.Instance == null)
        {
            var gmObject = new GameObject("GameManager");
            var gm = gmObject.AddComponent<GameManager>();

            gmObject.AddComponent<ControllerManager>();
            gmObject.AddComponent<ModelControllerManager>();
            gmObject.AddComponent<PresenterManager>();
            gmObject.AddComponent<ModifierManager>();
            gmObject.AddComponent<EffectManager>();
            gmObject.AddComponent<EffectRunner>();
            gmObject.AddComponent<SaveLoadManager>();
            gmObject.AddComponent<AutoSaveManager>();

            Debug.Log("[TestSceneSetup] GameManager created with all managers");
        }
    }

    private void SetupPlayer()
    {
        var playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "Player";
        playerObject.transform.position = Vector3.zero;

        var controller = playerObject.AddComponent<TestPlayerController>();

        var entity = playerObject.AddComponent<PlayerEntity>();

        var movementPart = playerObject.AddComponent<MovementPart>();
        var combatPart = playerObject.AddComponent<CombatPart>();

        Debug.Log("[TestSceneSetup] Player created with controller, entity, and parts");
    }

    private void SetupUI()
    {
        var canvas = CreateCanvas();

        var uiPresenterObject = new GameObject("TestUIPresenter");
        var uiPresenter = uiPresenterObject.AddComponent<TestUIPresenter>();

        var uiPanel = CreateUIPanel(canvas.transform);

        Debug.Log("[TestSceneSetup] UI system created");
    }

    private Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private GameObject CreateUIPanel(Transform parent)
    {
        var panel = new GameObject("UIPanel");
        panel.transform.SetParent(parent);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.7f);
        rect.anchorMax = new Vector2(0.3f, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 5;

        return panel;
    }

    private void SetupTestEnvironment()
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Ground";
        plane.transform.position = Vector3.zero;
        plane.transform.localScale = new Vector3(10, 1, 10);

        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        mainCamera.transform.position = new Vector3(0, 10, -10);
        mainCamera.transform.LookAt(Vector3.zero);

        var light = new GameObject("Directional Light");
        var lightComponent = light.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        lightComponent.transform.rotation = Quaternion.Euler(50, -30, 0);

        CreateInventoryController();
        CreateTestEnemies();

        Debug.Log("[TestSceneSetup] Test environment created");
    }

    private void CreateInventoryController()
    {
        var inventoryObject = new GameObject("InventoryController");
        inventoryObject.AddComponent<TestInventoryController>();

        Debug.Log("[TestSceneSetup] Inventory controller created");
    }

    private void CreateTestEnemies()
    {
        for (int i = 0; i < 3; i++)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyObject.name = $"Enemy_{i}";
            enemyObject.transform.position = new Vector3((i - 1) * 5, 0.5f, 5);

            enemyObject.AddComponent<TestEnemyController>();

            var rigidbody = enemyObject.AddComponent<Rigidbody>();
            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        Debug.Log("[TestSceneSetup] Test enemies created");
    }

    private void PrintInstructions()
    {
        Debug.Log(@"
=== TEST INSTRUCTIONS ===
Movement:
- WASD or Arrow Keys: Move player
- Space: Change state (Idle -> Moving -> Combat -> Idle)

Combat:
- Left Click: Attack
- H: Take 10 damage
- J: Heal 20 HP
- Q: Apply weapon buff (10 seconds)
- E: Apply poison effect (5 seconds)

UI:
- Tab: Toggle UI visibility

Save/Load:
- F5: Quick Save
- F9: Quick Load

Inventory:
- 1: Add Health Potion
- 2: Add Mana Potion  
- 3: Add 100 Gold
- 4: Spend 50 Gold
- I: Show Inventory

The system will automatically test:
1. Modifier System (health, attack, speed modifiers)
2. Effect System (timed buffs and debuffs)
3. State Machine (player states)
4. Flag System (ground check, attack availability)
5. Model-View binding
6. Entity-Part communication
7. Behavior Tree AI (3 enemies)
8. Save/Load system
9. Inventory management
=========================");
    }
}