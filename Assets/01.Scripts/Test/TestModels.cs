// ======== Test Models ========

using Akasha.Modifier;
using Akasha;
using static UnityEngine.EventSystems.EventTrigger;
using UnityEngine;

public class TestSimpleModel : BaseModel
{
    public RxVar<int> testValue;

    public TestSimpleModel()
    {
        testValue = new RxVar<int>(0, "testValue", this);
    }
}

public class TestUnitModel : BaseModel
{
    public RxVar<string> unitName;
    public RxMod<int> health;
    public RxMod<float> speed;
    public RxVar<bool> isActive;

    public TestUnitModel(string name = "TestUnit")
    {
        unitName = new RxVar<string>(name, "unitName", this);
        health = new RxMod<int>(100, "health", this);
        speed = new RxMod<float>(5.0f, "speed", this);
        isActive = new RxVar<bool>(true, "isActive", this);
    }
}

public class TestUIModel : BaseModel
{
    public RxVar<int> score;
    public RxVar<string> playerName;
    public RxVar<bool> gameStarted;

    public TestUIModel()
    {
        score = new RxVar<int>(0, "score", this);
        playerName = new RxVar<string>("Player1", this);
        gameStarted = new RxVar<bool>(false, "gameStarted", this);
    }
}
