using Akasha;
using UnityEngine;

public class TestSimpleEntity : BaseEntity<TestSimpleModel>
{
    protected override TestSimpleModel SetupModel()
    {
        return new TestSimpleModel();
    }

    protected override void AtModelReady()
    {
        Debug.Log($"[SimpleEntity] Model Ready - Test Value: {Model.testValue.Value}");
    }
}
