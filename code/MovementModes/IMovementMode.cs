namespace Sandbox.MovementModes;

public interface IMovementMode
{
	public bool ShouldRun();

	public void Init(PlayerCharacter player);
	public void CalcVelocity(ref Vector3 velocity);
	public void UpdateRotation();
}
