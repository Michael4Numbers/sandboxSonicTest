using System;

namespace Sandbox.MovementModes;

public class AirMovement : IMovementMode
{
	[Property] private float AirAccel { get; set; } = 1f;
	[Property] private float AirBraking { get; set; } = 1f;
	[Property, Range(0,1)] private float AirFriction { get; set; } = 1f;

	
	// really hacky code for deceleration to work properly with springs and not fuck with their trajectories
	public TimeUntil _timeUntilSpringTrajectoryOver = 0;

	public override bool EnterCondition()
	{
		return !_player.IsOnStableGround();
	}

	public override void PrePhysics()
	{
		_player.CalculateInputVector();
		
		// Calculate velocities (directly set it)
		Vector3 vel = _rb.Velocity;
		
		CalcVelocity( ref vel );
		
		// Set velocity accordingly
		_rb.Velocity = vel;
	}

	public override void PostPhysics()
	{
		// Find ground
		_player.EvaluateGroundingStatus();
		
		// Update gravity
		_player.GravityDir = _player.TargetGravDir;
		
		UpdateRotation();
	}

	public override void CalcVelocity(ref Vector3 velocity)
	{
		Vector3 vel2D = Vector3.VectorPlaneProject( velocity, _player.TargetGravDir );
		float ogPlanarSpeed = vel2D.Length;
		Vector3 velVertical = velocity.ProjectOnNormal( _player.TargetGravDir );
		
		// Apply gravity to vertical component
		velVertical += _player.GravityDir * Time.Delta;
		
		// Apply acceleration & friction to 2D
		Vector3 accelVector = _player.InputVector * AirAccel;

		// NOTE: Apply braking when no input kinda fucks with shit like the springs, so if we have a 'spring launch' state i can add it back in properly
		if ( accelVector.IsNearlyZero())
		{
			// Apply braking
			if ( _timeUntilSpringTrajectoryOver <= 0 )
			{
				vel2D = vel2D.Normal * MathF.Max( 0, vel2D.Length - AirBraking * Time.Delta );
			}
		}
		else
		{
			// Acceleration & Turning (damping directions not aligned with accelVector
			float turnAngle = Vector3.GetAngle( vel2D.Normal, accelVector.Normal );

			float brakingMultiplier = 1;
			if (turnAngle < 160)
			{
				vel2D = vel2D - (vel2D - accelVector.Normal * vel2D.Length) * Math.Min( Time.Delta * AirFriction, 1 );
			}
			else
			{
				//brakingMultiplier = 6;
				vel2D = vel2D.Normal * MathF.Max( 0, vel2D.Length - 4 * AirBraking * Time.Delta );
			}

			// Apply input acceleration if less than max speed (guessing 4000 but should prolly make it a parameter on _player so everyone can be consistent)
			Vector3 accelVel2D = (vel2D + accelVector * Time.Delta);//.ClampLength( ogPlanarSpeed );
			
			// Don't accelerate over 2000 units/s, so just clamp the length in that case but keep the direction
			if ( accelVel2D.Length < 1000 )
			{
				vel2D = accelVel2D;
			}
			else
			{
				vel2D = accelVel2D.Normal * vel2D.Length;
			}
			
		}
		
		// Recombine
		velocity = vel2D + velVertical;
		
		// Add gravity since no rigidbody gravity is applied
		velocity += _player.Gravity * Time.Delta;
	}

	public override void UpdateRotation()
	{
		Vector3 targetUp = -_player.GravityDir;
		Vector3 targetForward = _rb.Velocity.IsNearlyZero() ? _player.WorldRotation.Forward : _rb.Velocity.Normal;
		targetForward = targetForward.PlaneProject( targetUp ).Normal;

		Rotation targetRot = Rotation.LookAt( targetForward, targetUp );
		_player.WorldRotation = Rotation.Slerp( _player.WorldRotation, targetRot, 15f * Time.Delta );
	}
}
