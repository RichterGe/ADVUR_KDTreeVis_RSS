using Godot;

namespace KDTreeVis.Scene;

/// <summary>Shows the result of the last ray cast as an on-screen message at the bottom center.</summary>
public partial class RayResultOverlay : CanvasLayer
{
    private const int ScreenMargin = 12;
    private const float PanelPadding = 8f;
    private const int CornerRadius = 4;

    private PanelContainer _panel;
    private Label _label;

    public override void _Ready()
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

        _panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            GrowHorizontal = Control.GrowDirection.Both,
            GrowVertical = Control.GrowDirection.Begin,
            Visible = false,
        };
        _panel.AddThemeStyleboxOverride("panel", background);

        _label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        _label.AddThemeColorOverride("font_color", Colors.White);

        _panel.AddChild(_label);
        AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom, Control.LayoutPresetMode.Minsize, ScreenMargin);
    }

    /// <summary>Displays the message, replacing the previous one.</summary>
    public void ShowMessage(string message)
    {
        _label.Text = message;
        _panel.Visible = true;
    }
}
