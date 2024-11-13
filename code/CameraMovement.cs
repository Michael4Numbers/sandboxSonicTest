using Sandbox;

public sealed class CameraMovement : Component
{
	[Property] public GameObject Player { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public float Distance { get; set; } = 0f;

	[Property] public CameraComponent Camera;

	protected override void OnAwake()
	{

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
		var target = Player.WorldPosition + (Player.WorldRotation.Up * 32);
		WorldPosition = Vector3.Lerp(WorldPosition, target, 25f * Time.Delta);
		var speed = Player.GetComponent<Rigidbody>().Velocity.Length;
		var targetFov = MapRange( speed, 1000, 3000, 80, 90 ).Clamp( 80, 90 );
		Camera.FieldOfView = MathX.Lerp(Camera.FieldOfView, targetFov, 5 *  Time.Delta);
	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}
}
