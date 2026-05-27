using Godot;
using LaneWars.Core;

namespace LaneWars.UI;

public partial class EndGameOverlay : Control
{
    private Label _resultLabel = null!;
    private Label _planLabel = null!;
    private Label _timingLabel = null!;
    private Label _mistakeLabel = null!;
    private Label _adaptationLabel = null!;
    private Button _playAgainButton = null!;
    private PanelContainer _panel = null!;

    [Signal]
    public delegate void PlayAgainPressedEventHandler();

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("Panel");
        _resultLabel = GetNode<Label>("Panel/VBox/ResultLabel");
        _planLabel = GetNode<Label>("Panel/VBox/PlanLabel");
        _timingLabel = GetNode<Label>("Panel/VBox/TimingLabel");
        _mistakeLabel = GetNode<Label>("Panel/VBox/MistakeLabel");
        _adaptationLabel = GetNode<Label>("Panel/VBox/AdaptationLabel");
        _playAgainButton = GetNode<Button>("Panel/VBox/PlayAgainButton");
        _panel.AddThemeStyleboxOverride("panel", UiChrome.CreatePanelStyle(new Color(0.08f, 0.09f, 0.12f, 0.97f), contentMargin: 16.0f));
        UiChrome.ApplyFantasyButtonTheme(_playAgainButton);
        _playAgainButton.Pressed += () => EmitSignal(SignalName.PlayAgainPressed);
        Visible = false;
    }

    public void Show(int winner, int humanPlayer, PostgameAnalysis analysis)
    {
        if (winner == humanPlayer)
            _resultLabel.Text = "VICTORY!";
        else
            _resultLabel.Text = "DEFEAT";

        _planLabel.Text = $"AI Plan: {analysis.AiPlan}";
        _timingLabel.Text = analysis.KeyEnemyTiming;
        _mistakeLabel.Text = analysis.LikelyMistake;
        _adaptationLabel.Text = analysis.SuggestedAdaptation;
        Visible = true;
    }
}
