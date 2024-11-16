using Sandbox;
using Sandbox.Services;
using System;
using System.Diagnostics;
using Sandbox.MovementModes;

public static class VectorExtensions
{
	public static Vector3 PlaneProject( this Vector3 vector, Vector3 planeNormal )
	{
		planeNormal = planeNormal.Normal; // sanity
		return vector - planeNormal * vector.Dot( planeNormal );
	}
}

public sealed partial class PlayerCharacter : Component, IScenePhysicsEvents
{
	[Property]
	public Rigidbody rigid { get; set; }

	[Property]
	public CapsuleCollider capsuleCollider { get; set; } 
	
#region GRAVITY PARAMS
	
	[Property] public float GravityScale = 3;
	
	[Sync]
	public Vector3 GravityDir
	{
		get => _gravityDir.Normal;
		set
		{
			// Adventure style gravity where we 'slerp' it
			if ( IsOnStableGround() ) _gravityDir = value.Normal;
			else _gravityDir = Vector3.Slerp( _gravityDir, value.Normal, 10 * Time.Delta ).Normal;
		}
	}
	[Sync] public Vector3 TargetGravDir { get; set; } = Vector3.Down;
	
	[Sync]
	public Vector3 Gravity
	{
		get => Scene.PhysicsWorld.Gravity.Length * GravityDir * GravityScale;
	}

	private Vector3 _gravityDir = Vector3.Down;
	
#endregion
	
	[Property]
	public GameObject ball { get; set; }
	[Property]
	public SkinnedModelRenderer playermodel { get; set; }
	
	[Sync]
	public Vector3 InputVector { get; set; } = Vector3.Zero;

	private int maxBounces = 5;


	private float skinWidth = 0.015f;

	private float timeSinceLastJump = 0f;


	private bool jumped = false;

	private bool airDashed = false;

	//[Sync]
	private List<IMovementMode> _movementModes { get; set; }
	private IMovementMode _activeMovementMode { get; set; }

	protected override void OnAwake()
	{
		rigid = Components.Get<Rigidbody>();
	}

	protected override void OnStart()
	{
		base.OnStart();
		
		rigid.Gravity = true; // S&Box BUG! rigidbodies with Gravity = false just dont work properly it seems...
		rigid.PhysicsBody.GravityScale = 0;
		
		// Add movement modes
		_movementModes = GetComponentsInChildren<IMovementMode>().ToList();
		_activeMovementMode = GetComponentInChildren<GroundMovement>();
		
		// Initialize modes
		foreach (var mode in _movementModes)
		{
			mode.Init( this );
			mode.Enabled = false;
		}

		SetMovementMode<GroundMovement>();

		// example
		OnLanded += OnLandedListener;
	}

	void OnLandedListener()
	{
		// example
		Log.Info( "SONIC HAS LANDED!" );

		SetMovementMode<GroundMovement>();
	}

	protected override void OnPreRender()
	{
		base.OnPreRender();
		
		Gizmo.Draw.Color = ( Color.Orange );
		Gizmo.Draw.Arrow( GameObject.WorldPosition, GameObject.WorldPosition + GravityDir * 75, 8, 3 );
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		_activeMovementMode.PrePhysics( );
	}

	public void PostPhysicsStep()
	{
		_activeMovementMode.PostPhysics( );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
	}

	Vector3 CameraRelativeInput(Vector3 rawInput)
	{
		Vector3 targetNormal = IsOnStableGround() ? _groundingStatus.HitResult.Normal : -GravityDir;
		Rotation rotation = Scene.Camera.WorldRotation;
		
		Rotation planeRot = Rotation.FromToRotation( rotation.Up, targetNormal );
		Rotation orientedCamRotation = planeRot * rotation;
		Vector3 forward = orientedCamRotation.Forward.PlaneProject( targetNormal ).Normal;
		Vector3 right = orientedCamRotation.Right.PlaneProject( targetNormal ).Normal;
		
		return forward * rawInput.x + right * rawInput.y;
	}

	public void CalculateInputVector()
	{
		// Reset
		InputVector = Vector3.Zero;
		
		// Compute
		if ( Input.Down( "Forward" ) ) InputVector += Vector3.Forward;
		if ( Input.Down( "Backward" ) ) InputVector += Vector3.Backward;
		if ( Input.Down( "Left" ) ) InputVector -= Vector3.Left;
		if ( Input.Down( "Right" ) ) InputVector -= Vector3.Right;

		InputVector = CameraRelativeInput( InputVector );
		if ( !InputVector.IsNearZeroLength ) InputVector = InputVector.Normal;

	}

	#region Spindash Test

	public bool bSpinDashCharging { get; private set; } = false;
	TimeSince _timeSinceDashing = 0;
	public TimeUntil _timeUntilDashOver = 0;
	void TrySpinDash()
	{
		if ( Input.Pressed( "attack1" ) && IsOnStableGround() && !bSpinDashCharging && _timeUntilDashOver <= 0.4f)
		{
			bSpinDashCharging = true;
			ball.Enabled = true;
			playermodel.Tint = Color.Transparent;
			_timeSinceDashing = 0;
		}
		if ( bSpinDashCharging && Input.Released( "attack1" ) )
		{
			_timeUntilDashOver = 1f;

			Sound.Play( "player_airdash", WorldPosition );
			bSpinDashCharging = false;
			float boostAmount = float.Lerp( 900, 3000, Math.Clamp(_timeSinceDashing, 0, 1) );
			rigid.Velocity += (WorldRotation.Forward * boostAmount);
		}

		if ( Input.Pressed( "attack1" ) && _timeUntilDashOver > 0 ) _timeUntilDashOver = 0;

		if ( _timeUntilDashOver > 0 || bSpinDashCharging)
		{
			ball.Enabled = true;
			playermodel.Tint = Color.Transparent;
		}
	}
	
	#endregion
	
	protected override void OnUpdate()
	{
		timeSinceLastJump += Time.Delta;

		if ( Input.Pressed( "Jump" ) && IsOnStableGround() )
		{
			rigid.Velocity -= GravityDir * 1500; // jumps away from the floor
			Sound.Play( "player_jump", WorldPosition );
			Sound.Play( "player_jumproll", WorldPosition );
			ball.Enabled = true;
			playermodel.Tint = Color.Transparent;
			timeSinceLastJump = 0;
			jumped = true;
			UnGround();
		}
		else if ( IsOnStableGround() && timeSinceLastJump > 0.2f )
		{
			ball.Enabled = false;
			playermodel.Tint = Color.White;
			jumped = false;
			airDashed = false;
		}

		if ( Input.Pressed( "attack1" ) && !IsOnStableGround() && !airDashed )
		{
			Sound.Play( "player_airdash", WorldPosition );
			rigid.Velocity = (WorldRotation.Forward * 3000f).WithZ(0);
			airDashed = true;
			ball.Enabled = true;
			playermodel.Tint = Color.Transparent;
		}
		
		TrySpinDash();
	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}

	public void SetMovementMode<T>() where T : IMovementMode{
		_activeMovementMode.Enabled = false;
		_activeMovementMode = GameObject.GetComponentInChildren<T>(true);
		_activeMovementMode.Enabled = true;
	}
}
