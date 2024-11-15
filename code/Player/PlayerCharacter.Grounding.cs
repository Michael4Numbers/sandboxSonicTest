using Sandbox;
using System;

public struct FGroundingStatus
{
	public void SetFromHit( SceneTraceResult InHit )
	{
		HitResult = InHit;
		Angle = MathF.Acos( InHit.Normal.Dot( Vector3.Up ) ).RadianToDegree().NormalizeDegrees();
		Distance = InHit.Distance;
		bHasGround = InHit.Hit;
	}

	public bool bHasGround;
	public SceneTraceResult HitResult;
	public float Angle;
	public float Distance;
}


public sealed partial class PlayerCharacter : Component
{
	public Action OnLanded;
	
	public FGroundingStatus GroundingStatus { get => _groundingStatus; }
	private FGroundingStatus _groundingStatus;
	private FGroundingStatus _prevGroundingStatus;
	private TimeUntil _timeUntilReground = 0;

	void EvaluateGroundingStatus()
	{
		if ( _timeUntilReground > 0 ) return;
		
		SceneTraceResult rayHit = Scene.Trace.Ray(GameObject.WorldPosition + (-GravityDir * 10.0f), GameObject.WorldPosition + (GravityDir * 10.0f) )
			.Size( 5 )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();

		_prevGroundingStatus = _groundingStatus;
		_groundingStatus.SetFromHit( rayHit );

		// Compare ground results to see if we should eject from the ground on sharp angles
		if ( _groundingStatus.bHasGround && _prevGroundingStatus.bHasGround ) // BUG: Currently doesnt work as expected so disabling it, needs better ground tracing to work
		{
			float angleDelta = _prevGroundingStatus.Angle - _groundingStatus.Angle;
			if ( angleDelta > 20 ) // Hard coded denivelation angle
			{
				//_groundingStatus.bHasGround = false; // To not snap to the floor since its invalid
				//UnGround();
			}
		}

		if ( _groundingStatus.bHasGround )
		{
			/* Attempting to fix some snapping issues
			// Snap to ground
			float verticalDelta =
				(WorldPosition - _groundingStatus.HitResult.HitPosition).Dot( _groundingStatus.HitResult.Normal );

			// If negative, we need to snap down. Positive, we need to snap up. Goal is to be 2.5 inches above the ground
			verticalDelta = MathF.Min( verticalDelta, 2.5f );
			WorldPosition -= _groundingStatus.HitResult.Normal * verticalDelta;
			*/
			
			WorldPosition = _groundingStatus.HitResult.HitPosition - _groundingStatus.HitResult.Normal * 2.5f;
		}

		if ( !_prevGroundingStatus.bHasGround && _groundingStatus.bHasGround )
		{
			OnLanded?.Invoke();
		}
	}

	public bool IsOnStableGround() => _groundingStatus.bHasGround;

	public void UnGround()
	{
		_timeUntilReground = 0.3f; // Don't snap to floors for this duration
		_groundingStatus.bHasGround = false;
	}

}
