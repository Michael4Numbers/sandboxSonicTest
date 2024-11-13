using System;
using System.Numerics;
using Sandbox;

public sealed class CameraMovement : Component, IScenePhysicsEvents
{
	[Property, Hide] public GameObject Player { get; set; }
	[Property, Hide, RequireComponent] public CameraComponent Camera {get; set;}
	
	protected override void OnAwake()
	{
		GameObject parentObj = GameObject.Parent;
		foreach (var child in parentObj.Children)
		{
			if ( child.Tags.Has( "player" ) )
			{
				Player = child;
				break;
			}
		}

		_basis = Rotation.Identity;
	}

	[Property, Category("Camera Info")] public float Distance { get; set; } = 200f;
	[Property, Category("Camera Info")] public Vector2 LookSensitivity { get; set; } = 10f;
	[Property, Category("Camera Info")] public float PositionLerpSpeed { get; set; } = 25f;
	[Property, Category("Camera Info")] public float FaceMoveDirectionGracePeriod { get; set; } = 5f;

	[Property, Category("Floor Alignment")] 
	public bool bShouldCameraAlignToFloor { get; set; } = false;
	[Property, Category( "Floor Alignment" ), ShowIf( "bShouldCameraAlignToFloor", true )]
	public float FloorAlignmentSharpness = 5f;
	
	///	floor alignment rotation
	private Rotation _basis = Rotation.Identity;
	///	this frames look input delta
	private Angles _inputDelta;
	///	time since a look input was registered
	private TimeSince _sinceLastInput = 0;
	///	accumulated look input in players 'local basis'
	private Angles _camRot;
	
	///	camera spherical origin
	private Vector3 _cameraPivot;
	///	previous frames camera origin
	private Vector3 _prevCameraPivot;

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		CameraUpdate();
	}
	
	protected override void OnPreRender()
	{
		//CameraUpdate();
	}

	void CameraUpdate()
	{
		// Speed based FOV
		var target = Player.WorldPosition + (Player.WorldRotation.Up * 64);
		WorldPosition = Vector3.Lerp(WorldPosition, target, 25f * Time.Delta);
		var speed = Player.GetComponent<Rigidbody>().Velocity.Length;
		var targetFov = MapRange( speed, 1000, 3000, 80, 90 ).Clamp( 80, 90 );
		Camera.FieldOfView = MathX.Lerp(Camera.FieldOfView, targetFov, 5 *  Time.Delta);

		// Acknowledge look input
		_inputDelta.pitch = Input.MouseDelta.y * LookSensitivity.y * Time.Delta;
		_inputDelta.yaw = -Input.MouseDelta.x * LookSensitivity.x * Time.Delta;
		
		if (!_inputDelta.IsNearlyZero(  )) _sinceLastInput = 0;

		_camRot += _inputDelta;
		_camRot.pitch = _camRot.pitch.Clamp( -75f, 45f );
		
		// Smoothly turn to direction we're going
		FocusMoveDirection();
		
		Rotation targetRot = _camRot.ToRotation();
		
		_prevCameraPivot = _cameraPivot;
		_cameraPivot = PositionLerpSpeed == 0 ? target : Vector3.Lerp(_cameraPivot, target, PositionLerpSpeed * Time.Delta);
		
		// Basis
		if (bShouldCameraAlignToFloor)
		{
			Rotation basistarget = Rotation.FromToRotation( Vector3.Up, Player.WorldRotation.Up );
			_basis = Rotation.Slerp( _basis, basistarget, FloorAlignmentSharpness * Time.Delta );
			targetRot = _basis * targetRot;
		}

		// Spherical orbit camera
		WorldRotation = targetRot;
		WorldPosition = _cameraPivot - WorldRotation.Forward * Distance;
		
		// Trace
		var hit = Scene.Trace.Ray( _cameraPivot, WorldPosition ).Size(10f ).IgnoreGameObject( GameObject ).WithoutTags( "player" ).Run();

		WorldPosition = hit.EndPosition;
	}

	void FocusMoveDirection()
	{
		float OrbitFactor = Math.Clamp(_sinceLastInput / FaceMoveDirectionGracePeriod, 0f, 1f);

		Vector3 From = (_prevCameraPivot - WorldPosition).Normal;
		Vector3 To = (_cameraPivot - WorldPosition).Normal;

		Rotation RotDelta = Rotation.FromToRotation(From, To);
		_camRot.yaw += RotDelta.Angles().yaw * OrbitFactor;  
	}
	
	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}
}
