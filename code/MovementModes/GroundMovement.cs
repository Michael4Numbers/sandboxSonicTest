﻿using System;

namespace Sandbox.MovementModes;

public class GroundMovement : IMovementMode
{
	[Property] private float BrakingFriction { get; set; } = 900f;
	[Property] private float GroundSpeed { get; set; } = 1f;
	[Property, Range(0,360)] private float MaxSlopeAngle { get; set; } = 60f;
	
	public override bool EnterCondition()
	{
		return _player.IsOnStableGround();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		_rb.Velocity = Vector3.VectorPlaneProject( _rb.Velocity, _player.GroundingStatus.HitResult.Normal );
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

	public override void CalcVelocity(ref Vector3 velocity)
	{
		Vector3 targetVel = _player.bSpinDashCharging ? 0 : _player.InputVector * GroundSpeed; // zero input vector if charging a spindash

		var newSpeed = (velocity.Length + (GroundSpeed * 20 * PlayerCharacter.MapRange( velocity.Length, 0, 4000, 1, 0 )));

		// Limiting turn rate during the spindash grace period
		float turnRate = _player._timeUntilDashOver > 0 ? .05f : 0.2f;

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
			Vector3 gravOnPlane = _player.TargetGravDir.PlaneProject( _player.GroundingStatus.HitResult.Normal ) * _player.Gravity.Length;
			velocity += gravOnPlane * Time.Delta;
		}
	}
}
