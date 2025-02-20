using System;
using System.Numerics;

namespace Sandbox.MovementModes;

public class RailGrindMovement : IMovementMode
{
	public RailGrind RailGrind;

	float distance;
	float speed;

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
		float dotProd = Vector3.Dot( _player.WorldRotation.Forward, Vector3.Down );
		speed += dotProd * 10f * (speed > 0 ? 1 : -1);
		distance += speed * Time.Delta;
		if ( distance >= RailGrind.spline.Length || distance <= 0 )
		{
			EndGrind();
		}
		UpdatePlayerPos();
	}

	protected override void OnEnabled()
	{
		var convertedPos = RailGrind.Transform.World.ToLocal( _player.Transform.World );
		Spline.Sample sample = RailGrind.spline.SampleAtClosestPosition( convertedPos.Position );
		distance = sample.Distance;
		int direction = sample.Tangent.Dot( convertedPos.Rotation.Forward ) >= 0 ? 1 : -1 ;
		speed = _player.rigid.Velocity.Length * direction;
		UpdatePlayerPos();
		_player.SetBallMode( 0 );
		_player.capsuleCollider.IsTrigger = true;
		_player.rigid.Gravity = false;
		_player.rigid.Velocity = 0;
		RailGrind.GameObject.GetComponent<SplineCollider>(true).Enabled = false;
	}

	protected override void OnDisabled()
	{
		_player.capsuleCollider.IsTrigger = false;
		_player.rigid.Gravity = true;
		RailGrind.GameObject.GetComponent<SplineCollider>(true).Enabled = true;
	}

	public void EndGrind(bool jumped = false)
	{
		if ( jumped )
		{
			_player.SetBallMode( 1 );
			_player.rigid.Velocity = (_player.WorldRotation.Forward * MathF.Abs( speed )) + (_player.WorldRotation.Up * _player.JumpForce);
			_player._timeUntilCanGrind = 1f;
			_player.ClearAirDash();
		}
		else
		{
			_player.SetBallMode( 0 );
			_player.rigid.Velocity = (_player.WorldRotation.Forward * MathF.Abs( speed ));
			_player._timeUntilCanGrind = 1f;
			_player.SetMovementMode<AirMovement>();
			_player.UnGround();
		}
	}

	void UpdatePlayerPos()
	{
		var transform = RailGrind.GetTransformAtDistance( distance, speed );
		_player.WorldPosition = Vector3.Lerp( _player.WorldPosition, transform.Position, Time.Delta * 25 );
		_player.WorldRotation = transform.Rotation;
	}

}
