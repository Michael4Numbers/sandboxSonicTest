using Sandbox;

public sealed class PlayerNetworkManager : Component
{
	[Property] public GameObject UI;
	[Property] public GameObject Camera;
	protected override void OnStart()
	{
		if ( !IsProxy )
		{
			UI.Enabled = true;
			Camera.Enabled = true;
		} else
		{
			UI.Enabled = false;
			Camera.Enabled = false;
		}
	}
}
