using Sandbox;
using Sandbox.MovementModes;
using System.Linq;

public sealed class Ring : Component, Component.ITriggerListener
{
	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Contains<string>("player") )
		{
			other.GetComponent<PlayerCharacter>().rings += 1;
			Sound.Play( "ring", WorldPosition );
			GameObject.Destroy();
		}
	}
}
