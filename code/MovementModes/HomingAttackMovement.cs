using static Sandbox.PhysicsContact;

namespace Sandbox.MovementModes;

public class HomingAttackMovement : IMovementMode
{
	[Property] private float HomingSpeed { get; set; } = 1f;
	[Property] private float HomingAirLaunch { get; set; } = 1200f;
	[Property] private float HomingForwardLaunch { get; set; } = 2000f;

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

		homingLerp += Time.Delta * HomingSpeed * 500;

		if ( homingLerp >= homingDistance )
		{
			Vector3 launchDirection = Vector3.Direction( homingStart, homingEnd );
			_player.rigid.Velocity = (launchDirection.WithZ( 0f ).Normal * HomingForwardLaunch) + (-_player.GravityDir * HomingAirLaunch);
			_player.ClearAirDash();
			_player.homingTarget = null;
			_player.ball.Enabled = false;
			_player.playermodel.Tint = Color.White;
			_player.playermodel.Set( "homingAttack", true );
			_player.SetMovementMode<AirMovement>();
		}

		_player.WorldPosition = Vector3.Lerp(homingStart, homingEnd, homingLerp / homingDistance);
		
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
