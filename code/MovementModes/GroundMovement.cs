namespace Sandbox.MovementModes;

public class GroundMovement : IMovementMode
{
	[Property] private float GroundSpeed { get; set; } = 1f;
	[Property, Range(0,360)] private float MaxSlopeAngle { get; set; } = 60f;
	
	public override bool EnterCondition()
	{
		return _player.IsOnStableGround();
	}

	public override void PrePhysics()
	{
		_player.CalculateInputVector();
		
		// Calculate velocities (directly set it)
		Vector3 vel = _rb.Velocity;
		
		CalcVelocity( ref vel );
		
		// Set velocity accordingly
		_rb.Velocity = vel;

		_player.TryStep();
	}

	public override void PostPhysics()
	{
		// Find ground
		_player.EvaluateGroundingStatus();

		if(!_player.IsOnStableGround()){
			_player.SetMovementMode<AirMovement>();
		}
		
		if (_player.GroundingStatus.Angle > MaxSlopeAngle){
			Log.Info(_player.GroundingStatus.Angle);
			
			_player.UnGround();
		}
			
		
		// Update gravity
		_player.GravityDir = -_player.GroundingStatus.HitResult.Normal;
		
		
		UpdateRotation();

		// Revert step (this is done in S&Boxs PlayerController)
		//_player.RestoreStep();
	}

	public override void CalcVelocity(ref Vector3 velocity)
	{
		Vector3 targetVel = _player.bSpinDashCharging ? 0 : _player.InputVector * GroundSpeed; // zero input vector if charging a spindash
		
		var newSpeed = (velocity.Length + (GroundSpeed * 20 * PlayerCharacter.MapRange( velocity.Length, 0, 4000, 1, 0 )));

		// Limiting turn rate during the spindash grace period
		float turnRate = _player._timeUntilDashOver > 0 ? .05f : 0.2f;

		if ( targetVel.Length > 0 )
		{
			var initDirection = velocity.Normal;
			var targetDirection = targetVel;
			velocity = (Vector3.Slerp( initDirection, targetDirection, turnRate ) * newSpeed);
		}
		else if (_player._timeUntilDashOver <= 0) // NOTE: Just removing braking when we're still in the spindash grace period
		{
			velocity = (velocity * 0.95f);
		}
		
		// Slope physics
		float slopeSlideVelThreshold = 400;
		if ( velocity.Length > slopeSlideVelThreshold )
		{
			// Add slope physics, but only in the sense that we speed up/slow down not change directions
			if ( velocity.Length > 4000 ) return;
		
			float dH = (velocity * Time.Delta).Dot( _player.TargetGravDir );
			float dV = float.Sign( dH ) * float.Sqrt( 10 * float.Abs(dH) );
			velocity = velocity.Normal * (velocity.Length + dV);
		}
		else
		{
			// Apply gravity directly if we're not moving so we slide down slopes if standing still [optional]
			Vector3 gravOnPlane = _player.TargetGravDir.PlaneProject( _player.GroundingStatus.HitResult.Normal ) * _player.Gravity.Length;
			velocity += gravOnPlane * Time.Delta;
		}
	}

	public override void UpdateRotation()
	{
		Vector3 targetUp = _player.GroundingStatus.HitResult.Normal;
		// Bit of a high tolerance but because theres gonna always be some subtle movement unless we apply braking frction, equiv to 1.2 m/s
		Vector3 targetForward = _rb.Velocity.IsNearlyZero(50) ? _player.WorldRotation.Forward : _rb.Velocity.Normal;

		if ( _player.bSpinDashCharging  && _player.InputVector.Length > 0)
		{
			targetForward = _player.InputVector;
		}

		targetForward = Vector3.VectorPlaneProject( targetForward, targetUp ).Normal;

		
		Rotation targetRot = Rotation.LookAt( targetForward, targetUp );
		_player.WorldRotation = Rotation.Slerp( _player.WorldRotation, targetRot, 15f * Time.Delta );
	}
}
