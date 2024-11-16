using static Sandbox.PhysicsContact;

namespace Sandbox.MovementModes;

public class HomingAttackMovement : IMovementMode
{
	[Property] private float HomingSpeed { get; set; } = 1f;

	private float homingDistance { get; set; }
	private float homingLerp { get; set; }
	
	private Vector3 homingStart { get; set; }
	private Vector3 homingEnd { get; set; }

	public override bool EnterCondition()
	{
		return !_player.IsOnStableGround();
	}

	public override void PrePhysics()
	{
		
	}

	public override void PostPhysics()
	{
		
	}

	protected override void OnUpdate(){

		homingLerp += Time.Delta * HomingSpeed;

		_player.WorldPosition = Vector3.Lerp(homingStart, homingEnd, homingLerp);

		if ( homingLerp > 1f )
		{
			Vector3 launchDirection = Vector3.Direction( homingStart, homingEnd );
			_player.rigid.Velocity = (launchDirection.WithZ( 0f ).Normal * 2000) + (Vector3.Up * 1000f);
			_player.ClearAirDash();
			_player.homingTarget = null;
			_player.ball.Enabled = false;
			_player.playermodel.Tint = Color.White;
			_player.playermodel.Set( "homingAttack", true );
			_player.SetMovementMode<AirMovement>();
		}
		
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		homingDistance = Vector3.DistanceBetween( _player.WorldPosition, _player.homingTarget.WorldPosition );
		homingLerp = 0;
		homingStart = _player.WorldPosition;
		homingEnd = _player.homingTarget.WorldPosition;
	}


}
