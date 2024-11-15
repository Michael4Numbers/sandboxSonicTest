namespace Sandbox.MovementModes;

public abstract class IMovementMode : Component
{
	protected PlayerCharacter _player;
	protected Rigidbody _rb;
	
	public abstract bool EnterCondition();

	public bool IsEnabled()
	{
		return Active;
	}
	
	public void Init( PlayerCharacter player )
	{
		_player = player;
		_rb = _player.rigid;
	}

	// OnEnabled / OnUpdate / OnDisabled is the equivalent of Start/Tick/End so no need to define them here

	public abstract void PrePhysics();
	public abstract void PostPhysics();

	public virtual void CalcVelocity( ref Vector3 velocity ) { }
	public virtual void UpdateRotation() { }
}

// NOTE: This component is literally just for the custom editor widget lmfao
public class MovementModeManager : Component
{
	
}
