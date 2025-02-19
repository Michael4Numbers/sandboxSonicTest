using Sandbox;
using Sandbox.MovementModes;
using System;

public sealed partial class PlayerCharacter
{

    private bool airDashed = false;

	public GameObject homingTarget;

	public Sphere lastSphere;
	public Sphere lastEndSphere;

	GameObject TraceHomingTarget(){

        GameObject homingTarget;

		Vector3 homingDirection = InputVector.Length > 0 ? Vector3.Lerp( InputVector, GameObject.WorldRotation.Forward, 0.3f ).Normal : GameObject.WorldRotation.Forward;

		Vector3 startTrace = GameObject.WorldPosition + (homingDirection * 200.0f);
		Vector3 endTrace = startTrace + (homingDirection * 1000.0f);

		IEnumerable<SceneTraceResult> boxHits = Scene.Trace.Sphere( 400f, startTrace, endTrace )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.WithTag( "targetable" )
			.HitTriggers()
			.RunAll();

		lastSphere = new Sphere( startTrace, 400f);
		lastEndSphere = new Sphere( endTrace, 400f );

		homingTarget = null;

        float distanceToTarget = 99999f;

        foreach (SceneTraceResult boxHit in boxHits){
           // Log.Info(boxHit.GameObject.Name);
            if(boxHit.Distance < distanceToTarget){
                homingTarget = boxHit.GameObject;
				distanceToTarget = boxHit.Distance;

			}
        }

		return homingTarget;
    }

	

    void AttemptHomingAttack(){

        homingTarget = TraceHomingTarget();

        if(homingTarget != null){
            HomingAttack(homingTarget);
        }
        else
        {
            AirDash();
        }
    }

    void AirDash(){
        Sound.Play( "player_airdash", WorldPosition );
		rigid.Velocity = (WorldRotation.Forward * 3000f).WithZ(0);
		airDashed = true;
		SetBallMode( 1 );
	}

    void HomingAttack(GameObject target){
        Sound.Play( "player_airdash", WorldPosition );
		homingTarget = target;
		airDashed = true;
		SetBallMode( 1 );
		SetMovementMode<HomingAttackMovement>();
		IHomingAttackTarget.PostToGameObject( homingTarget, x => x.HomingAttackInit( this, WorldPosition, homingTarget.WorldPosition ) );
    }

	public void ClearAirDash()
	{
		airDashed = false;
	}
}
