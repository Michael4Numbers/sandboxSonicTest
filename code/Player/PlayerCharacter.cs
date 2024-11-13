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

	[Property]
	public float speed { get; set; } = 1f;
	
	[Property]
	public GameObject ball { get; set; }
	[Property]
	public SkinnedModelRenderer playermodel { get; set; }

	[Sync]
	public Vector3 wishVelocity { get; set; } = Vector3.Zero;
	
	[Sync]
	public Vector3 InputVector { get; set; } = Vector3.Zero;

	private int maxBounces = 5;


	private float skinWidth = 0.015f;

	private float timeSinceLastJump = 0f;


	private bool jumped = false;

	private bool airDashed = false;

	[Sync]
	private List<IMovementMode> _movementModes { get; set; }

	protected override void OnAwake()
	{
		rigid = Components.Get<Rigidbody>();
		
		// Add movement modes
		_movementModes = new List<IMovementMode>();
		_movementModes.Add( new GroundMovement() );
		_movementModes.Add( new AirMovement() );
		
		// Initialize modes
		foreach (var mode in _movementModes)
		{
			mode.Init( this );
		}
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		CalculateInputVector();
		
		// Calculate velocities (directly set it)
		Vector3 vel = rigid.Velocity;
		// Run the appropriate calc velocity
		foreach ( var mode in _movementModes )
		{
			if (mode.ShouldRun())
				mode.CalcVelocity( ref vel );
		}
		// Set velocity accordingly
		rigid.Velocity = vel;

		TryStep();
	}

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		// Find ground
		EvaluateGroundingStatus();
		
		// Update our rotation now that we're done with everything
		// Run the appropriate calc velocity
		foreach ( var mode in _movementModes )
		{
			if (mode.ShouldRun())
				mode.UpdateRotation( );
		}
		//UpdateRotation();
		
		// Revert step (this is done in S&Boxs PlayerController)
		RestoreStep();
		
		// Update gravity based on grounding status
		if ( IsOnStableGround() )
		{
			rigid.PhysicsBody.GravityScale = 0;
		}
		else
		{
			rigid.PhysicsBody.GravityScale = 3f;
		}
	}

	Vector3 CameraRelativeInput(Vector3 rawInput)
	{
		Vector3 targetNormal = IsOnStableGround() ? _groundingStatus.HitResult.Normal : new Vector3( 0, 0, 1 );
		Rotation rotation = Scene.Camera.WorldRotation;
		
		Rotation planeRot = Rotation.FromToRotation( rotation.Up, targetNormal );
		Rotation orientedCamRotation = planeRot * rotation;
		Vector3 forward = orientedCamRotation.Forward.PlaneProject( targetNormal ).Normal;
		Vector3 right = orientedCamRotation.Right.PlaneProject( targetNormal ).Normal;
		
		return forward * rawInput.x + right * rawInput.y;
	}

	void CalculateInputVector()
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
	
	private bool bSpinDashCharging = false;
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
			rigid.Velocity = rigid.Velocity.WithZ( rigid.Velocity.z + 1500 );
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
}
