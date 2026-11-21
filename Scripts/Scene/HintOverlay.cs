using Godot;

namespace KDTreeVis.Scene;

public partial class HintOverlay : CanvasLayer
{
    private const int ScreenMargin = 12;
    private const float PanelPadding = 8f;
    private const int CornerRadius = 4;

    [Export] public string HintText { get; set; } = "Toggle k-d tree: Enter";

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

        var panel = new PanelContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            GrowHorizontal = Control.GrowDirection.Begin,
        };
        panel.AddThemeStyleboxOverride("panel", background);

        var label = new Label
        {
            Text = HintText,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeColorOverride("font_color", Colors.White);

        panel.AddChild(label);
        AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight, Control.LayoutPresetMode.Minsize, ScreenMargin);
    }
}
