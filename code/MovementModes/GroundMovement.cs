using System;

namespace Sandbox.MovementModes;

// Helper functions ported from Unity
public static class Vector3Extensions
{
	public static Vector3 MoveTowards(this Vector3 current, Vector3 target, float maxDistanceDelta)
	{
		// Calculate displacement vector
		Vector3 toVector = target - current;
		float dist = toVector.Length;

		// Already closer than maxDistanceDelta - return target
		if (dist <= maxDistanceDelta || dist < float.Epsilon)
		{
			return target;
		}

		// Move in direction of target by maxDistanceDelta units
		return current + toVector * (maxDistanceDelta / dist);
	}
	
    public static Vector3 RotateTowards(this Vector3 from, Vector3 to, float maxRadiansDelta, float maxMagnitudeDelta)
    {
        // Handle zero vectors or identical vectors
        float fromMag = from.Length;
        float toMag = to.Length;
        
        if (fromMag < 0.001f || toMag < 0.001f)
        {
            // Interpolate magnitude only
            return from.MoveTowards(to, maxMagnitudeDelta);
        }

        // Normalize vectors
        Vector3 fromDir = from / fromMag;
        Vector3 toDir = to / toMag;

        // If vectors are identical, skip rotation
        float dot = Vector3.Dot(fromDir, toDir);
        if (dot > 0.9999f)
        {
            // Just handle magnitude change
            return from.MoveTowards(to, maxMagnitudeDelta);
        }

        // Calculate angle between vectors
        float angle = MathF.Acos(float.Clamp(dot, -1f, 1f));
        
        // If we're already within maxRadiansDelta, just return target direction
        if (angle <= maxRadiansDelta)
        {
            // Still need to handle magnitude
            return (toDir * fromMag).MoveTowards(
                  // Rotated but keeping original magnitude
                to,              // Target vector with desired magnitude
                maxMagnitudeDelta
            );
        }

        // Calculate rotation axis
        Vector3 axis = Vector3.Cross(fromDir, toDir);
        if (axis.Length < 0.001f)
        {
            // Vectors are parallel but opposite
            // Find any perpendicular axis
            axis = Vector3.Cross(fromDir, Vector3.Up);
            if (axis.Length < 0.001f)
            {
                axis = Vector3.Cross(fromDir, Vector3.Right);
            }
        }

        axis = axis.Normal;

        // Create rotation quaternion for maxRadiansDelta
        Rotation rotation = Rotation.FromAxis(axis, maxRadiansDelta.RadianToDegree());
        
        // Rotate our normalized direction
        Vector3 newDir = rotation * fromDir;
        
        // Scale to current magnitude and handle magnitude delta
        return (newDir * fromMag).MoveTowards(
            to,               // Target vector
            maxMagnitudeDelta
        );
    }
}

public class GroundMovement : IMovementMode
{
	[Property] private float BrakingFriction { get; set; } = 900f;
	[Property] private float GroundSpeed { get; set; } = 1f;
	[Property, Range(0,360)] private float MaxSlopeAngle { get; set; } = 60f;
	
	[Property, Group("New Method")] private float MaxSpeed = 4000;
	[Property, Group("New Method")] private float Acceleration = 900f;
	[Property, Group("New Method")] private float BrakeForce = 600f;
	[Property, Group("New Method")] private float turnDecceleration = 1200f;

	[Property] private float GroundMaxSpeed { get; set; } = 3000f;
	[Property] private float GroundAccel { get; set; } = 2048f;
	[Property] private float GroundBraking { get; set; } = 2500f;
	[Property, Range( 0, 1 )] private float GroundFriction { get; set; } = 15f;

	[Property, Group("New Method")] private float minTurnSpeed = 400;
	//[Property, Group("New Method")] private float maxTurnRate = 600;

	public override bool EnterCondition()
	{
		return _player.IsOnStableGround();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		if( _rb != null )
		{
			_rb.Velocity = Vector3.VectorPlaneProject( _rb.Velocity, _player.GroundingStatus.HitResult.Normal );
		}
	}

	public override void PrePhysics()
	{
		_player.CalculateInputVector();

		// Calculate velocities (directly set it)
		Vector3 vel = _rb.Velocity;
		
		if (_player.IsOnStableGround())
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
			return;
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

	public Vector3 NewCalculateVelocity( Vector3 velocity, Vector3 moveInput, float deltaTime )
	{
		// Apply acceleration & friction to 2D
		Vector3 accelVector = _player.InputVector * GroundAccel;

		if ( accelVector.IsNearlyZero() )
		{
			// Apply braking
			if ( _player.bSpinDashCharging || _player._timeUntilDashOver <= 0 )
			{
				velocity = velocity.Normal * MathF.Max( 0, velocity.Length - GroundBraking * Time.Delta );
			}
		}
		else
		{
			// Acceleration & Turning (damping directions not aligned with accelVector
			float turnAngle = Vector3.GetAngle( velocity.Normal, _player.InputVector );

			float brakingMultiplier = 1;
			if ( turnAngle < 115 )
			{
				velocity = velocity - (velocity - accelVector.Normal * velocity.Length) * Math.Min( Time.Delta * GroundFriction, 1 );
			}
			else
			{
				// Double the braking if we're trying to move in reverse
				velocity = velocity.Normal * MathF.Max( 0, velocity.Length - 2 * GroundBraking * Time.Delta );
			}

			// Apply input acceleration if less than max speed (guessing 4000 but should prolly make it a parameter on _player so everyone can be consistent)
			Vector3 accelVel2D = (velocity + accelVector * Time.Delta);//.ClampLength( ogPlanarSpeed );

			// Don't allow input acceleration to take us over the max speed, its ok for input to change directions when we've exceeded max speed tho
			if ( accelVel2D.Length < GroundMaxSpeed )
			{
				velocity = accelVel2D;
			}
			else
			{
				velocity = accelVel2D.Normal * velocity.Length;
			}

		}

		return velocity;
	}

	public override void CalcVelocity(ref Vector3 velocity)
	{
		Vector3 targetVel = _player.bSpinDashCharging ? 0 : _player.InputVector; // zero input vector if charging a spindash

		/*velocity = NewCalculateVelocity( velocity, targetVel, Time.Delta );
		ApplySlopePhysics( ref velocity );
		return;*/

		var newSpeed = (velocity.Length + (GroundSpeed * 20 * PlayerCharacter.MapRange( velocity.Length, 0, 4000, 1, 0 )));

		// Limiting turn rate during the spindash grace period
		float turnRateWalk = MapRange(velocity.Length, 0, 3000, 0.2f, 0.1f );
		float turnRate = _player._timeUntilDashOver > 0 ? .05f : turnRateWalk;

		float inputAngle = velocity.IsNearlyZero(  ) ? 0 : Vector3.GetAngle( _player.InputVector, velocity.Normal );
		
		if ( targetVel.Length > 0  && inputAngle < 160 ) // another arbitrary magic number, 20 degree window
		{
			var initDirection = Vector3.VectorPlaneProject(velocity.Normal, _player.GroundingStatus.HitResult.Normal).Normal;
			var targetDirection = Vector3.VectorPlaneProject(targetVel, _player.GroundingStatus.HitResult.Normal).Normal;
			velocity = (Vector3.Slerp( initDirection, targetDirection, turnRate ) * newSpeed);
		}
		else if (_player._timeUntilDashOver <= 0) // NOTE: Just removing braking when we're still in the spindash grace period
		{
			float brakingMultiplier = targetVel.Length > 0 ? 4 : 1;
			ApplyBraking( ref velocity , brakingMultiplier);
		}
		
		ApplySlopePhysics( ref velocity );
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

	void ApplyBraking( ref Vector3 velocity , float brakingMultiplier = 1)
	{
		velocity = velocity.Normal * Math.Max(0, velocity.Length - brakingMultiplier * BrakingFriction * Time.Delta);
	}

	void ApplySlopePhysics( ref Vector3 velocity )
	{
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
			//Vector3 gravOnPlane = _player.TargetGravDir.PlaneProject( _player.GroundingStatus.HitResult.Normal ) * _player.Gravity.Length;
			//velocity += gravOnPlane * Time.Delta;
		}
	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax, bool clamp = true )
	{
		if ( clamp )
		{
			return MathX.Clamp( ((value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin), outMin, outMax );
		}
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}
}
