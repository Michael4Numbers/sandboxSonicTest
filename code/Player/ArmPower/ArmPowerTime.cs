using Sandbox;
using Sandbox.MovementModes;
using System.Numerics;

public class ArmPowerTime : ArmPower
{
	[Property] float heightDifference = 15840.0f;
	bool isToggled;
	FilmGrain grain;
	protected override void OnAwake()
	{
		base.OnAwake();
		grain = _player.cam.AddComponent<FilmGrain>();
		grain.Intensity = 0.0f;
		grain.Response = 0.0f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		grain.Intensity = MathX.Lerp( grain.Intensity, 0.0f, 5f * Time.Delta );
	}

	public override void PowerOn()
	{
		canBeActivated = false;
		if ( !isToggled )
		{
			_player.playermodel.Set( "activate_power", true );
			Sound.Play( "InstaShield" );
			Teleport( -heightDifference );
		}
		else
		{
			PowerOff();
		}
	}

	async void Teleport(float height)
	{
		await Task.Delay(200);
		grain.Intensity = 0.5f;
		_player.cam.TeleportCam(WorldPosition, WorldPosition - new Vector3( 0, 0, height ) );
		_player.WorldPosition += new Vector3( 0, 0, height );
		Sound.Play( "MightyLand" );
		isToggled = !isToggled;
		canBeActivated = true;
	}

	public override void PowerOff()
	{
		_player.playermodel.Set( "activate_power", true );
		Sound.Play( "InstaShield" );
		Teleport( heightDifference );
	}

}
