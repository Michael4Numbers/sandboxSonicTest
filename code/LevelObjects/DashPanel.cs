using Sandbox;

public sealed class DashPanel : Component, Component.ITriggerListener
{
	[Property] float speed = 3000f;
	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Contains<string>( "player" ) )
		{
			var player = other.GetComponent<PlayerCharacter>();
			player.rigid.Velocity = speed * WorldRotation.Forward;
			Sound.Play( "dashpanel", WorldPosition.WithZ(WorldPosition.z + 20) );
		}
	}
}
