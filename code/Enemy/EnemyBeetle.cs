using Sandbox;

public class EnemyBeetle : EnemyBase, IHomingAttackTarget
{
	[Property] GameObject destroyedObject;

	Vector3 deathDir;

	public override void Death()
	{
		var obj = destroyedObject.Clone( WorldPosition, WorldRotation );
		var launcher = obj.GetComponent<DeadEnemyLauncher>();
		launcher.launchDirection = deathDir;
		launcher.launchSpeed = 2000f;
	}

	void IHomingAttackTarget.HomingAttackHit( PlayerCharacter player, Vector3 homingStart, Vector3 homingEnd )
	{
		deathDir = Vector3.Direction(homingStart, homingEnd);
		Sound.Play( "impact_metal", WorldPosition );
		Death();
		DestroyGameObject();
	}

	void IHomingAttackTarget.HomingAttackInit( PlayerCharacter player, Vector3 homingStart, Vector3 homingEnd )
	{

	}
}
