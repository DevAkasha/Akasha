using UnityEngine;
using Akasha;

public class TestUIPresenter : BasePresenter
{
    private TestUIView uiView;
    private PlayerModel playerModel;

    protected override void OnPresenterInitialize()
    {
        Debug.Log("[TestUIPresenter] Initializing UI presenter");

        var playerController = GameManager.Instance.GetModelController<TestPlayerController>();
        if (playerController != null)
        {
            playerModel = playerController.Model;
            SetupUI();
        }
        else
        {
            Debug.LogError("[TestUIPresenter] Player controller not found!");
        }
    }

    private void SetupUI()
    {
        uiView = CreateView<TestUIView>();
        if (uiView != null && playerModel != null)
        {
            uiView.BindToModel(playerModel);
            Show();
        }
    }

    protected override void OnShow()
    {
        Debug.Log("[TestUIPresenter] UI Shown");
    }

    protected override void OnHide()
    {
        Debug.Log("[TestUIPresenter] UI Hidden");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (HasViews)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }
}