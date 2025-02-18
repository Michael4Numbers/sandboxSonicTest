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

	[Property] public CameraMovement cam;

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
	
	[Property] public ModelRenderer ball { get; set; }
	[Property] public SkinnedModelRenderer groundBall { get; set; }
	[Property] public SkinnedModelRenderer playermodel { get; set; }
	
	[Sync]
	public Vector3 InputVector { get; set; } = Vector3.Zero;


	public float levelTimer = 0f;

	public int rings = 0;


	private int maxBounces = 5;

	private float skinWidth = 0.015f;

	private float timeSinceLastJump = 0f;

	private bool jumped = false;

	

	//[Sync]
	private List<IMovementMode> _movementModes { get; set; }
	[Sync] private IMovementMode _activeMovementMode { get; set; }
	[Sync] public IMovementMode movementMode { get => _activeMovementMode; }

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
		_movementModes = GetComponentsInChildren<IMovementMode>(true).ToList();
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
	
	#region Debugging

	[Property, Hide, ConVar("vizTrajectory", Help = "Draw players trajectory in game", Min = 0, Max = 1, Saved = true)]
	private static bool bDisplayTrajectory { get; set; } = false;
	//List<Vector3> positionHistory = new List<Vector3>(400);
	private Queue<Vector3> positionHistory = new Queue<Vector3>( 200 );

	void DebugFixedUpdate()
	{
		if (positionHistory.Count > 200) positionHistory.Dequeue();
		positionHistory.Enqueue( WorldPosition - TargetGravDir * 35 );
	}
	
	protected override void OnPreRender()
	{
		base.OnPreRender();

		Gizmo.Draw.Color = (Color.Orange);
		Gizmo.Draw.Arrow( GameObject.WorldPosition, GameObject.WorldPosition + GravityDir * 75, 8, 3 );
		
		if ( bDisplayTrajectory )
		{
			var posList = positionHistory.ToList();

			for ( int i = 0; i < posList.Count - 1; i++ )
			{
				Gizmo.Draw.LineThickness = 10;
				Gizmo.Draw.Line( posList[i], posList[i + 1] );
			}
		}
	}

	#endregion 
	
	void IScenePhysicsEvents.PrePhysicsStep()
	{
		if ( IsProxy ) return;
		_activeMovementMode.PrePhysics( );
	}

	public void PostPhysicsStep()
	{
		if ( IsProxy ) return;
		_activeMovementMode.PostPhysics( );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( IsProxy ) return;
		DebugFixedUpdate(); // Updates various debug values
	}

	Vector3 CameraRelativeInput(Vector3 rawInput)
	{
		Vector3 targetNormal = IsOnStableGround() ? _groundingStatus.HitResult.Normal : -TargetGravDir;
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
		if ( !Input.UsingController )
		{
			if ( Input.Down( "Forward" ) ) InputVector += Vector3.Forward;
			if ( Input.Down( "Backward" ) ) InputVector += Vector3.Backward;
			if ( Input.Down( "Left" ) ) InputVector -= Vector3.Left;
			if ( Input.Down( "Right" ) ) InputVector -= Vector3.Right;
		}
		else
		{
			InputVector += Vector3.Forward * Input.AnalogMove.x;
			InputVector += Vector3.Right * Input.AnalogMove.y;
		}

		InputVector = CameraRelativeInput( InputVector );
		if ( !InputVector.IsNearZeroLength ) InputVector = InputVector.Normal;

	}

	#region Spindash Test

	public bool bSpinDashCharging { get; private set; } = false;
	public TimeSince _timeSinceDashing = 0;
	public TimeUntil _timeUntilDashOver = 0;
	void TrySpinDash()
	{
		if ( Input.Pressed( "attack1" ) && IsOnStableGround() && !bSpinDashCharging && _timeUntilDashOver <= 0.4f)
		{
			bSpinDashCharging = true;
			SetBallMode( 2 );
			_timeSinceDashing = 0;
			Sound.Play( "player_spindash_start", WorldPosition );
		}
		if ( bSpinDashCharging && Input.Released( "attack1" ) )
		{
			_timeUntilDashOver = 1f;

			Sound.Play( "player_spindash_release", WorldPosition );
			bSpinDashCharging = false;
			float boostAmount = float.Lerp( 900, 2500, Math.Clamp(_timeSinceDashing, 0, 2) );
			rigid.Velocity += (WorldRotation.Forward * boostAmount);
		}

		if ( Input.Pressed( "attack1" ) && _timeUntilDashOver > 0 ) _timeUntilDashOver = 0;

		if ( _timeUntilDashOver > 0 || bSpinDashCharging)
		{
			SetBallMode( 2 );
		}
	}
	
	#endregion
	
	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		levelTimer += Time.Delta;
		timeSinceLastJump += Time.Delta;

		if ( Input.Pressed( "Jump" ) && IsOnStableGround() )
		{
			rigid.Velocity -= GravityDir * 1500; // jumps away from the floor
			Sound.Play( "player_jump", WorldPosition );
			Sound.Play( "player_jumproll", WorldPosition );
			SetBallMode( 1 );
			timeSinceLastJump = 0;
			jumped = true;
			UnGround();
		}
		else if ( IsOnStableGround() && timeSinceLastJump > 0.2f )
		{
			SetBallMode( 0 );
			jumped = false;
			airDashed = false;
		}

		if ( Input.Pressed( "attack1" ) && !IsOnStableGround() && !airDashed )
		{
			AttemptHomingAttack();
		}

		TrySpinDash();
	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}

	public void SetMovementMode<T>() where T : IMovementMode{
		IMovementMode componentGrab = GameObject.GetComponentInChildren<T>( true );
		if ( componentGrab != null )
		{
			_activeMovementMode.Enabled = false;
			_activeMovementMode = componentGrab;
			_activeMovementMode.Enabled = true;
		}
	}
	
	public T GetMovementMode<T>() where T : IMovementMode{
		return GameObject.GetComponentInChildren<T>( true );
	}

	public void SetBallMode( int ballType )
	{
		if ( ballType == 0 )
		{
			groundBall.Tint = Color.Transparent;
			ball.Tint = Color.Transparent;
			playermodel.Tint = Color.White;
		}
		else if ( ballType == 1 )
		{
			groundBall.Tint = Color.Transparent;
			ball.Tint = Color.White;
			playermodel.Tint = Color.Transparent;
		}
		else
		{
			groundBall.Tint = Color.White;
			ball.Tint = Color.Transparent;
			playermodel.Tint = Color.Transparent;
		}
	}
}
