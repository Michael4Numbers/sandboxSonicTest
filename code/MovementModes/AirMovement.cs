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
		_rb.PhysicsBody.GravityScale = 3f;
		
		Vector3 targetVel = _player.InputVector * _player.speed;
		
		var newSpeed = (velocity.Length + (_player.speed * 20 * PlayerCharacter.MapRange( velocity.Length, 0, 4000, 1, 0 )));

		// Limiting turn rate during the spindash grace period
		float turnRate = _player._timeUntilDashOver > 0 ? .05f : 0.2f;

		var initDirection = velocity.WithZ( 0 ).Normal;
		var targetDirection = targetVel;
			
		if(targetVel.Length > 0 )
		{
			velocity = (Vector3.Slerp( initDirection, targetDirection, .1f ) * velocity.WithZ(0).Length).WithZ(velocity.z);
		}
	}

	public void UpdateRotation()
	{
		Vector3 targetUp = Vector3.Up;
		Vector3 targetForward = _rb.Velocity.IsNearlyZero() ? _player.WorldRotation.Forward : _rb.Velocity.Normal;
		targetForward = targetForward.PlaneProject( targetUp ).Normal;

		Rotation targetRot = Rotation.LookAt( targetForward, targetUp );
		_player.WorldRotation = Rotation.Slerp( _player.WorldRotation, targetRot, 15f * Time.Delta );
	}
}
