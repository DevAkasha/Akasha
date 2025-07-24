using UnityEngine;
using UnityEngine.UI;
using Akasha;

public class TestUIView : BaseView
{
    [Header("UI Elements")]
    [SerializeField] private Text healthText;
    [SerializeField] private Text attackText;
    [SerializeField] private Text speedText;
    [SerializeField] private Text stateText;
    [SerializeField] private Text flagsText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text nameText;

    private PlayerViewModel viewModel;

    protected override void SetupComponents()
    {
        viewModel = CreateViewModel<PlayerViewModel>();

        if (healthText == null) healthText = CreateTextElement("Health: 0");
        if (attackText == null) attackText = CreateTextElement("Attack: 0");
        if (speedText == null) speedText = CreateTextElement("Speed: 0");
        if (stateText == null) stateText = CreateTextElement("State: Idle");
        if (flagsText == null) flagsText = CreateTextElement("Flags: ");
        if (levelText == null) levelText = CreateTextElement("Level: 1");
        if (nameText == null) nameText = CreateTextElement("Name: Player");
    }

    public void BindToModel(PlayerModel model)
    {
        viewModel.Bind(model);

        viewModel.HealthSlot.AddValueChangeListener(value => {
            healthText.text = $"Health: {value}";
        });

        viewModel.AttackSlot.AddValueChangeListener(value => {
            attackText.text = $"Attack: {value}";
        });

        viewModel.SpeedSlot.AddValueChangeListener(value => {
            speedText.text = $"Speed: {value:F1}";
        });

        viewModel.StateSlot.AddValueChangeListener(value => {
            var state = (PlayerState)value;
            stateText.text = $"State: {state}";
            stateText.color = GetStateColor(state);
        });

        viewModel.FlagsSlot.AddValueChangeListener(value => {
            UpdateFlagsDisplay();
        });

        viewModel.LevelSlot.AddValueChangeListener(value => {
            levelText.text = $"Level: {value}";
        });

        viewModel.NameSlot.AddValueChangeListener(value => {
            nameText.text = $"Name: {value}";
        });
    }

    private void UpdateFlagsDisplay()
    {
        var flags = viewModel.FlagsSlot.Value;
        if (flags == null) return;

        string flagsStr = "Flags: ";
        flagsStr += $"Grounded[{(flags.GetValue(PlayerFlag.IsGrounded) ? "O" : "X")}] ";
        flagsStr += $"CanAttack[{(flags.GetValue(PlayerFlag.CanAttack) ? "O" : "X")}] ";
        flagsStr += $"Invulnerable[{(flags.GetValue(PlayerFlag.IsInvulnerable) ? "O" : "X")}] ";
        flagsStr += $"HasWeapon[{(flags.GetValue(PlayerFlag.HasWeapon) ? "O" : "X")}]";

        flagsText.text = flagsStr;
    }

    private Color GetStateColor(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle: return Color.white;
            case PlayerState.Moving: return Color.green;
            case PlayerState.Combat: return Color.yellow;
            case PlayerState.Dead: return Color.red;
            default: return Color.gray;
        }
    }

    private Text CreateTextElement(string defaultText)
    {
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(transform);
        var text = textGO.AddComponent<Text>();
        text.text = defaultText;
        text.fontSize = 14;
        text.color = Color.white;
        return text;
    }

    protected override void OnShow()
    {
        Debug.Log("[TestUIView] View shown");
    }

    protected override void OnHide()
    {
        Debug.Log("[TestUIView] View hidden");
    }
}