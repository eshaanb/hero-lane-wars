using Godot;

namespace LaneWars.UI;

public partial class EndGameOverlay : Control
{
    private Label _resultLabel = null!;
    private Button _playAgainButton = null!;

    [Signal]
    public delegate void PlayAgainPressedEventHandler();

    public override void _Ready()
    {
        _resultLabel = GetNode<Label>("Panel/VBox/ResultLabel");
        _playAgainButton = GetNode<Button>("Panel/VBox/PlayAgainButton");
        _playAgainButton.Pressed += () => EmitSignal(SignalName.PlayAgainPressed);
        Visible = false;
    }

    public void Show(int winner, int humanPlayer)
    {
        if (winner == humanPlayer)
            _resultLabel.Text = "VICTORY!";
        else
            _resultLabel.Text = "DEFEAT";
        Visible = true;
    }
}
