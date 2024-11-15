namespace Sandbox.MovementModes;

public class AirMovement : IMovementMode
{
	private PlayerCharacter _player;
	private Rigidbody _rb;

	public bool ShouldRun()
	{
		return !_player.IsOnStableGround();
	}

	public void Init( PlayerCharacter player )
	{
		_player = player;
		_rb = _player.rigid;
	}

	public void CalcVelocity(ref Vector3 velocity)
	{
		Vector3 targetVel = _player.InputVector * _player.speed;

		var initDirection = velocity.WithZ( 0 ).Normal;
		var targetDirection = targetVel;
			
		if(targetVel.Length > 0 )
		{
			velocity = (Vector3.Slerp( initDirection, targetDirection, .1f ) * velocity.WithZ(0).Length).WithZ(velocity.z);
		}
		
		// Add gravity since no rigidbody gravity is applied
		velocity += _player.Gravity * Time.Delta;
	}

	public void UpdateRotation()
	{
		Vector3 targetUp = -_player.GravityDir;
		Vector3 targetForward = _rb.Velocity.IsNearlyZero() ? _player.WorldRotation.Forward : _rb.Velocity.Normal;
		targetForward = targetForward.PlaneProject( targetUp ).Normal;

		Rotation targetRot = Rotation.LookAt( targetForward, targetUp );
		_player.WorldRotation = Rotation.Slerp( _player.WorldRotation, targetRot, 15f * Time.Delta );
	}
}
