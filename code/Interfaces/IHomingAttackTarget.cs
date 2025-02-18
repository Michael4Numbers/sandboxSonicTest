public interface IHomingAttackTarget : ISceneEvent<IHomingAttackTarget>
{
	void HomingAttackInit( PlayerCharacter player, Vector3 homingStart, Vector3 homingEnd );
	void HomingAttackHit( PlayerCharacter player, Vector3 homingStart, Vector3 homingEnd );
}
