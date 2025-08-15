using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Akasha.Modifier;

namespace Akasha.Test
{
    public class TestProjectManager : ManagerBase
    {
        [Header("Test Configuration")]
        public bool runAllTests = true;
        public float testInterval = 5f;
        public int maxPoolTestObjects = 5;

        [Header("Test Prefabs")]
        public TestSimpleController simpleControllerPrefab;
        public TestUnitController unitControllerPrefab;
        public TestComplexController complexControllerPrefab;
        public TestUIPresenter uiPresenterPrefab;

        [Header("UI References")]
        public Button startTestButton;
        public Button poolTestButton;
        public Button modifierTestButton;
        public Button clearPoolButton;
        public Text statusText;

        public override int InitializationPriority => 90;

        private TimerHandle testTimerHandle;
        private int currentTestPhase = 0;
        private List<TestUnitController> spawnedUnits = new List<TestUnitController>();

        protected override void OnManagerAwake()
        {
            Debug.Log("[TestProjectManager] Test Manager Initialized");
            SetupUI();

            if (runAllTests)
            {
                StartCoroutine(DelayedAutoTest());
            }
        }

        private IEnumerator DelayedAutoTest()
        {
            yield return new WaitForSeconds(1f);
            StartComprehensiveTest();
        }

        private void SetupUI()
        {
            if (startTestButton != null)
                startTestButton.onClick.AddListener(StartComprehensiveTest);

            if (poolTestButton != null)
                poolTestButton.onClick.AddListener(RunPoolTest);

            if (modifierTestButton != null)
                modifierTestButton.onClick.AddListener(RunModifierTest);

            if (clearPoolButton != null)
                clearPoolButton.onClick.AddListener(ClearAllPools);

            UpdateStatusText("Test Manager Ready");
        }

        public void StartComprehensiveTest()
        {
            Debug.Log("=== STARTING COMPREHENSIVE AKASHA FRAMEWORK TEST ===");
            UpdateStatusText("Starting comprehensive test...");

            currentTestPhase = 0;
            testTimerHandle = this.RepeatingCall(testInterval, RunNextTestPhase);
        }

        private void RunNextTestPhase()
        {
            switch (currentTestPhase)
            {
                case 0:
                    TestPhase1_SimpleController();
                    break;
                case 1:
                    TestPhase2_UnitController();
                    break;
                case 2:
                    TestPhase3_ComplexController();
                    break;
                case 3:
                    TestPhase4_UIPresenter();
                    break;
                case 4:
                    TestPhase5_PoolOperations();
                    break;
                case 5:
                    TestPhase6_ModifierSystem();
                    break;
                case 6:
                    TestPhase7_SaveSystem();
                    break;
                case 7:
                    TestPhase8_Cleanup();
                    break;
                default:
                    CompleteTest();
                    return;
            }

            currentTestPhase++;
        }

        private void TestPhase1_SimpleController()
        {
            Debug.Log("=== PHASE 1: Simple Controller Test ===");
            UpdateStatusText("Phase 1: Testing Simple Controllers");

            if (simpleControllerPrefab != null)
            {
                var controller = GameManager.Controllers.SpawnControllerFromPrefab(simpleControllerPrefab);
                controller.transform.position = new Vector3(-2, 0, 0);

                this.DelayedCall(2f, () => {
                    if (controller != null)
                    {
                        GameManager.Controllers.ReturnController(controller);
                        Debug.Log("Simple controller returned to pool");
                    }
                });
            }
        }

        private void TestPhase2_UnitController()
        {
            Debug.Log("=== PHASE 2: Unit Controller Test ===");
            UpdateStatusText("Phase 2: Testing Unit Controllers");

            spawnedUnits.Clear();

            for (int i = 0; i < 3; i++)
            {
                var unit = Instantiate(unitControllerPrefab);
                unit.transform.position = new Vector3(i * 2, 0, 0);
                spawnedUnits.Add(unit);

                int index = i;
                this.DelayedCall(1f + index * 0.5f, () => {
                    if (unit != null) unit.TakeDamage();
                });

                this.DelayedCall(2f + index * 0.5f, () => {
                    if (unit != null) unit.Heal();
                });
            }
        }

        private void TestPhase3_ComplexController()
        {
            Debug.Log("=== PHASE 3: Complex Controller Test ===");
            UpdateStatusText("Phase 3: Testing Complex Controllers");

            if (complexControllerPrefab != null)
            {
                var complex = GameManager.Controllers.SpawnControllerFromPrefab(complexControllerPrefab);
                complex.transform.position = new Vector3(0, 2, 0);

                this.DelayedCall(1f, () => {
                    complex.ModifyUnitHealth(25f);
                });

                this.DelayedCall(2f, () => {
                    complex.ModifyUnitHealth(-15f);
                });
            }
        }

        private void TestPhase4_UIPresenter()
        {
            Debug.Log("=== PHASE 4: UI Presenter Test ===");
            UpdateStatusText("Phase 4: Testing UI Presenters");

            if (uiPresenterPrefab != null)
            {
                var presenter = GameManager.Presenters.SpawnPresenterFromPrefab(uiPresenterPrefab);

                this.DelayedCall(0.5f, () => {
                    presenter.SetPlayerName("TestPlayer");
                });

                this.DelayedCall(1f, () => {
                    presenter.StartGame();
                });

                this.DelayedCall(3f, () => {
                    presenter.StopGame();
                });
            }
        }

        private void TestPhase5_PoolOperations()
        {
            Debug.Log("=== PHASE 5: Pool Operations Test ===");
            UpdateStatusText("Phase 5: Testing Pool Operations");

            RunPoolTest();
        }

        private void TestPhase6_ModifierSystem()
        {
            Debug.Log("=== PHASE 6: Modifier System Test ===");
            UpdateStatusText("Phase 6: Testing Modifier System");

            RunModifierTest();
        }

        private void TestPhase7_SaveSystem()
        {
            Debug.Log("=== PHASE 7: Save System Test ===");
            UpdateStatusText("Phase 7: Testing Save System");

            foreach (var unit in spawnedUnits)
            {
                if (unit != null)
                {
                    unit.CallSave();
                }
            }
        }

        private void TestPhase8_Cleanup()
        {
            Debug.Log("=== PHASE 8: Cleanup Test ===");
            UpdateStatusText("Phase 8: Testing Cleanup");

            foreach (var unit in spawnedUnits)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            spawnedUnits.Clear();

            this.DelayedCall(1f, () => {
                var stats = GameManager.Instance.GetSystemStatistics();
                foreach (var stat in stats)
                {
                    Debug.Log($"System Stat: {stat.Key} = {stat.Value}");
                }
            });
        }

        private void CompleteTest()
        {
            Debug.Log("=== COMPREHENSIVE TEST COMPLETED ===");
            UpdateStatusText("All tests completed!");

            testTimerHandle?.Cancel();
            GameManager.Instance.PrintSystemStatus();
        }

        public void RunPoolTest()
        {
            Debug.Log("=== POOL TEST START ===");
            UpdateStatusText("Running pool test...");

            StartCoroutine(PoolTestCoroutine());
        }

        private IEnumerator PoolTestCoroutine()
        {
            var testObjects = new List<TestUnitController>();

            Debug.Log("Creating multiple objects...");
            for (int i = 0; i < maxPoolTestObjects; i++)
            {
                var obj = Instantiate(unitControllerPrefab);
                obj.transform.position = new Vector3(i * 1.5f, -2, 0);
                testObjects.Add(obj);
                yield return new WaitForSeconds(0.2f);
            }

            yield return new WaitForSeconds(2f);

            Debug.Log("Destroying test objects...");
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(1f);

            Debug.Log("Creating new objects...");
            testObjects.Clear();
            for (int i = 0; i < maxPoolTestObjects; i++)
            {
                var obj = Instantiate(unitControllerPrefab);
                obj.transform.position = new Vector3(i * 1.5f, -4, 0);
                testObjects.Add(obj);
                yield return new WaitForSeconds(0.2f);
            }

            UpdateStatusText("Pool test completed");
            Debug.Log("=== POOL TEST END ===");
        }

        public void RunModifierTest()
        {
            Debug.Log("=== MODIFIER TEST START ===");
            UpdateStatusText("Running modifier test...");

            if (spawnedUnits.Count > 0)
            {
                var testUnit = spawnedUnits[0];
                if (testUnit != null)
                {
                    Debug.Log($"Original Health: {testUnit.Model.health.Value}");

                    testUnit.AddHealthModifier("Buff1", 50f);
                    Debug.Log($"After Buff1: {testUnit.Model.health.Value}");

                    testUnit.AddHealthModifier("Buff2", 25f);
                    Debug.Log($"After Buff2: {testUnit.Model.health.Value}");

                    testUnit.RemoveHealthModifier("Buff1");
                    Debug.Log($"After removing Buff1: {testUnit.Model.health.Value}");

                    testUnit.RemoveHealthModifier("Buff2");
                    Debug.Log($"After removing all modifiers: {testUnit.Model.health.Value}");
                }
            }

            UpdateStatusText("Modifier test completed");
            Debug.Log("=== MODIFIER TEST END ===");
        }

        public void ClearAllPools()
        {
            Debug.Log("=== CLEARING ALL POOLS ===");
            UpdateStatusText("Clearing all pools...");

            foreach (var unit in spawnedUnits)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            spawnedUnits.Clear();

            UpdateStatusText("All pools cleared");
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[Status] {message}");
        }

        protected override void OnManagerDestroy()
        {
            testTimerHandle?.Cancel();
        }

        void Update()
        {
            UnityTimer.Tick(Time.deltaTime);
        }
    }
}