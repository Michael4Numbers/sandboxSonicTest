using Sandbox;
using Sandbox.Services;
using System;
using System.Diagnostics;

public static class VectorExtensions
{
	public static Vector3 PlaneProject( this Vector3 vector, Vector3 planeNormal )
	{
		planeNormal = planeNormal.Normal; // sanity
		return vector - planeNormal * vector.Dot( planeNormal );
	}
}

public struct FGroundingStatus
{
	public void SetFromHit( SceneTraceResult InHit )
	{
		HitResult = InHit;
		Angle = MathF.Acos( InHit.Normal.Dot( Vector3.Up ) ).RadianToDegree().NormalizeDegrees();
		Distance = InHit.Distance;
		bHasGround = InHit.Hit;
	}

	public bool bHasGround;
	public SceneTraceResult HitResult;
	public float Angle;
	public float Distance;
}


public sealed class PlayerCharacter : Component, IScenePhysicsEvents
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

	public Vector3 wishVelocity = Vector3.Zero;

	private int maxBounces = 5;


	private float skinWidth = 0.015f;

	private float timeSinceLastJump = 0f;


	private bool jumped = false;

	private bool airDashed = false;

	protected override void OnAwake()
	{
		rigid = Components.Get<Rigidbody>();
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		// Calculate velocities (directly set it)
		BuildWishVelocity();
		var newSpeed = (rigid.Velocity.Length + (speed * 20 * MapRange( rigid.Velocity.Length, 0, 4000, 1, 0 )));

		// Limiting turn rate during the spindash grace period
		float turnRate = _timeUntilDashOver > 0 ? .05f : 0.2f;

		if ( IsOnStableGround() )
		{
			if ( wishVelocity.Length > 0 )
			{
				//rigid.Velocity = (wishVelocity * newSpeed);
				var initDirection = rigid.Velocity.Normal;
				var targetDirection = wishVelocity;
				rigid.Velocity = (Vector3.Slerp( initDirection, targetDirection, turnRate ) * newSpeed);
			}
			else if (_timeUntilDashOver <= 0) // NOTE: Just removing braking when we're still in the spindash grace period
			{
				rigid.Velocity = (rigid.Velocity * 0.95f);
			}
		}
		else
		{
			var initDirection = rigid.Velocity.WithZ( 0 ).Normal;
			var targetDirection = wishVelocity;
			
			if(wishVelocity.Length > 0 )
			{
				rigid.Velocity = (Vector3.Slerp( initDirection, targetDirection, .1f ) * rigid.Velocity.WithZ(0).Length).WithZ(rigid.Velocity.z);
			}
			
		}

		
	}

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		// Find ground
		EvaluateGroundingStatus();
		UpdateRotation();
		
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

	void CameraRelativeInput(ref Vector3 rawInput)
	{
		Vector3 targetNormal = IsOnStableGround() ? _groundingStatus.HitResult.Normal : new Vector3( 0, 0, 1 );
		Rotation rotation = Scene.Camera.WorldRotation;
		
		Rotation planeRot = Rotation.FromToRotation( rotation.Up, targetNormal );
		Rotation orientedCamRotation = planeRot * rotation;
		Vector3 forward = orientedCamRotation.Forward.PlaneProject( targetNormal ).Normal;
		Vector3 right = orientedCamRotation.Right.PlaneProject( targetNormal ).Normal;
		
		rawInput = forward * rawInput.x + right * rawInput.y;
	}

	void BuildWishVelocity()
	{
		wishVelocity = 0;
		if ( bSpinDashCharging ) return;

		var rot = Rotation.Identity;
		if ( Input.Down( "Forward" ) ) wishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) wishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) wishVelocity -= rot.Left;
		if ( Input.Down( "Right" ) ) wishVelocity -= rot.Right;

		CameraRelativeInput( ref wishVelocity );
		if ( !wishVelocity.IsNearZeroLength ) wishVelocity = wishVelocity.Normal;

		wishVelocity *= speed;
	}
	
	void UpdateRotation()
	{
		Vector3 targetUp = IsOnStableGround() ? _groundingStatus.HitResult.Normal : Vector3.Up;
		Vector3 targetForward = rigid.Velocity.IsNearlyZero() ? WorldRotation.Forward : rigid.Velocity.Normal;
		targetForward = targetForward.PlaneProject( targetUp ).Normal;

		Rotation targetRot = Rotation.LookAt( targetForward, targetUp );
		WorldRotation = Rotation.Slerp( WorldRotation, targetRot, 15f * Time.Delta );
	}

	#region Spindash Test
	
	private bool bSpinDashCharging = false;
	TimeSince _timeSinceDashing = 0;
	private TimeUntil _timeUntilDashOver = 0;
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
	
	#region GROUNDING

	private FGroundingStatus _groundingStatus;
	private FGroundingStatus _prevGroundingStatus;
	private TimeUntil _timeUntilReground = 0;

	void EvaluateGroundingStatus()
	{
		if ( _timeUntilReground > 0 ) return;
		
		SceneTraceResult rayHit = Scene.Trace.Ray(GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();

		_prevGroundingStatus = _groundingStatus;
		_groundingStatus.SetFromHit( rayHit );

		// Compare ground results to see if we should eject from the ground on sharp angles
		if ( _groundingStatus.bHasGround && _prevGroundingStatus.bHasGround )
		{
			float angleDelta = _prevGroundingStatus.Angle - _groundingStatus.Angle;
			if ( angleDelta > 20 ) // Hard coded denivelation angle
			{
				_groundingStatus.bHasGround = false; // To not snap to the floor since its invalid
				UnGround();
			}
		}

		if ( _groundingStatus.bHasGround )
		{
			/* Attempting to fix some snapping issues
			// Snap to ground
			float verticalDelta =
				(WorldPosition - _groundingStatus.HitResult.HitPosition).Dot( _groundingStatus.HitResult.Normal );
			
			// If negative, we need to snap down. Positive, we need to snap up. Goal is to be 2.5 inches above the ground
			verticalDelta = MathF.Min( verticalDelta, 2.5f );
			WorldPosition -= _groundingStatus.HitResult.Normal * verticalDelta;
			*/
			
			WorldPosition = _groundingStatus.HitResult.HitPosition - _groundingStatus.HitResult.Normal * 2.5f;
		}
	}

	bool IsOnStableGround() => _groundingStatus.bHasGround;

	void UnGround()
	{
		_timeUntilReground = 0.3f; // Don't snap to floors for this duration
		_groundingStatus.bHasGround = false;
	}

	#endregion

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}
}
