using Godot;

namespace KDTreeVis.Scene;

public partial class FlyCamera : Camera3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float SprintMultiplier { get; set; } = 3.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.002f; // radians per pixel
    [Export] public float MinSpeed { get; set; } = 0.5f;
    [Export] public float MaxSpeed { get; set; } = 100.0f;

    // Input actions, bound in Project Settings > Input Map.
    private static readonly StringName MoveForward = "move_forward";
    private static readonly StringName MoveBack = "move_back";
    private static readonly StringName MoveLeft = "move_left";
    private static readonly StringName MoveRight = "move_right";
    private static readonly StringName MoveUp = "move_up";
    private static readonly StringName MoveDown = "move_down";
    private static readonly StringName Sprint = "sprint";
    private static readonly StringName SpeedUp = "speed_up";
    private static readonly StringName SpeedDown = "speed_down";
    private static readonly StringName CaptureMouse = "capture_mouse";
    private static readonly StringName ReleaseMouse = "release_mouse";

    private float _yaw;   // rotation around global Y
    private float _pitch; // rotation around local X

    public override void _Ready()
    {
        foreach (var action in new[] { MoveForward, MoveBack, MoveLeft, MoveRight, MoveUp, MoveDown,
                                       Sprint, SpeedUp, SpeedDown, CaptureMouse, ReleaseMouse })
        {
            if (!InputMap.HasAction(action))
                GD.PushError($"FlyCamera: Input action '{action}' is missing in the Input Map.");
        }

        // Start from whatever orientation the camera has in the editor.
        _yaw = Rotation.Y;
        _pitch = Rotation.X;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch -= motion.Relative.Y * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
            Rotation = new Vector3(_pitch, _yaw, 0f);
        }
        else if (@event.IsActionPressed(CaptureMouse))
            Input.MouseMode = Input.MouseModeEnum.Captured;
        else if (@event.IsActionPressed(ReleaseMouse))
            Input.MouseMode = Input.MouseModeEnum.Visible;
        else if (@event.IsActionPressed(SpeedUp))
            MoveSpeed = Mathf.Min(MoveSpeed * 1.1f, MaxSpeed);
        else if (@event.IsActionPressed(SpeedDown))
            MoveSpeed = Mathf.Max(MoveSpeed / 1.1f, MinSpeed);
    }

    public override void _Process(double delta)
    {
        // x = right, y = back (Godot cameras look along -Z).
        Vector2 planar = Input.GetVector(MoveLeft, MoveRight, MoveForward, MoveBack);
        float vertical = Input.GetAxis(MoveDown, MoveUp);

        if (planar == Vector2.Zero && Mathf.IsZeroApprox(vertical))
            return;

        float speed = MoveSpeed;
        if (Input.IsActionPressed(Sprint))
            speed *= SprintMultiplier;

        // Forward/right follow the view direction (incl. pitch), up/down is always world-up.
        var basis = GlobalTransform.Basis;
        Vector3 direction = basis.X * planar.X + basis.Z * planar.Y + Vector3.Up * vertical;

        GlobalPosition += direction.LimitLength() * speed * (float)delta;
    }
}
