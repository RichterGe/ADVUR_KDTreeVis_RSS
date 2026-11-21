using Godot;

namespace KDTreeVis.Scene;

/// <summary>
/// Displays the controls hint in the top-right corner and the latest ray cast result at the bottom center.
/// </summary>
public partial class HudOverlay : CanvasLayer
{
    private const int ScreenMargin = 12;
    private const float PanelPadding = 8f;
    private const int CornerRadius = 4;

    [Export(PropertyHint.MultilineText)]
    public string HintText { get; set; } = "Toggle k-d tree: Enter\nCast ray: Left click";

    private PanelContainer _resultPanel;
    private Label _resultLabel;

    public override void _Ready()
    {
        (_, Label hintLabel) = CreateBadge(
            Control.LayoutPreset.TopRight,
            Control.GrowDirection.Begin,
            Control.GrowDirection.End);
        hintLabel.Text = HintText;

        (_resultPanel, _resultLabel) = CreateBadge(
            Control.LayoutPreset.CenterBottom,
            Control.GrowDirection.Both,
            Control.GrowDirection.Begin);
        _resultPanel.Visible = false;
    }

    /// <summary>Displays the ray cast status message at the bottom center, replacing the previous one.</summary>
    public void ShowRayResult(string message)
    {
        _resultLabel.Text = message;
        _resultPanel.Visible = true;
    }

    private (PanelContainer Panel, Label Label) CreateBadge(
        Control.LayoutPreset preset,
        Control.GrowDirection growHorizontal,
        Control.GrowDirection growVertical)
    {
        var background = new StyleBoxFlat
        {
            BgColor = new Color(0f, 0f, 0f, 0.75f),
            ContentMarginLeft = PanelPadding,
            ContentMarginRight = PanelPadding,
            ContentMarginTop = PanelPadding,
            ContentMarginBottom = PanelPadding,
        };
        background.SetCornerRadiusAll(CornerRadius);

        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            GrowHorizontal = growHorizontal,
            GrowVertical = growVertical,
        };
        panel.AddThemeStyleboxOverride("panel", background);

        var label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color", Colors.White);

        panel.AddChild(label);
        AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(preset, Control.LayoutPresetMode.Minsize, ScreenMargin);

        return (panel, label);
    }
}
