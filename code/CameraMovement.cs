using Sandbox;

public sealed class CameraMovement : Component
{
	[Property] public GameObject Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 0f;

	private CameraComponent Camera;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
	}

	protected override void OnUpdate()
	{
		var eyeAngles = Head.WorldRotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.WorldRotation = eyeAngles.ToRotation();
	}

	protected override void OnPreRender()
	{
		WorldPosition = Player.WorldPosition + (Player.WorldRotation.Up * 32);
	}
}
