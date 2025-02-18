using System;

namespace Sandbox.MovementModes;

public class RailGrindMovement : IMovementMode
{
	public RailGrind RailGrind;

	float distance;
	float speed;
	
	// really hacky code for deceleration to work properly with springs and not fuck with their trajectories
	public TimeUntil _timeUntilSpringTrajectoryOver = 0;

	public override bool EnterCondition()
	{
		return true;
	}

	public override void PrePhysics()
	{

	}

	public override void PostPhysics()
	{

	}

	protected override void OnUpdate()
	{
		distance += speed * Time.Delta;
		UpdatePlayerPos();
	}

	protected override void OnEnabled()
	{
		Spline.Sample sample = RailGrind.spline.SampleAtClosestPosition( _player.WorldPosition );
		distance = sample.Distance;
		Log.Info( distance );
		int direction = sample.Tangent.Dot( _player.WorldRotation.Forward ) >= 0 ? 1 : -1 ;
		speed = _player.rigid.Velocity.Length * direction;
		UpdatePlayerPos();
	}

	void UpdatePlayerPos()
	{
		_player.WorldTransform = RailGrind.GetTransformAtDistance( distance );
	}

}
