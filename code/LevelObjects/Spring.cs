using Sandbox;
using Sandbox.MovementModes;
using System;

public sealed class Spring : Component, Component.ITriggerListener
{
	[Property] public float LaunchSpeed = 1500f;

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Contains<string>( "player" ) )
		{
			//Log.Info( other );
			var player = other.GetComponent<PlayerCharacter>();
			player.rigid.Velocity = WorldRotation.Up * LaunchSpeed;
			player.WorldPosition = WorldPosition + WorldRotation.Up * 64f;
			player.ClearAirDash();
			player.ball.Enabled = false;
			player.playermodel.Tint = Color.White;
			Sound.Play( "levelobject_spring", WorldPosition );
			player.SetMovementMode<AirMovement>();
		}
	}
}
