using Sandbox;
using System.Numerics;

public abstract class ArmPower : Component
{
	protected bool canBeActivated = true;

	protected PlayerCharacter _player;
	protected Rigidbody _rb;

	protected override void OnAwake()
	{
		base.OnAwake();
		_player = GetComponent<PlayerCharacter>();
		_rb = _player.rigid;
	}

	protected override void OnUpdate()
	{
		if ( Input.Pressed( "ActivatePower" ) && canBeActivated )
		{
			PowerOn();
		}
	}

	public abstract void PowerOn();

	public abstract void PowerOff();
}
