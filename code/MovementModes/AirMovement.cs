namespace Sandbox.MovementModes;

public class AirMovement : IMovementMode
{
	[Property] private float AirSpeed { get; set; } = 1f;

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
		Vector3 targetVel = _player.InputVector * AirSpeed;

		var initDirection = velocity.WithZ( 0 ).Normal;
		var targetDirection = targetVel;
			
		if(targetVel.Length > 0 )
		{
			velocity = (Vector3.Slerp( initDirection, targetDirection, .1f ) * velocity.WithZ(0).Length).WithZ(velocity.z);
		}
		
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
