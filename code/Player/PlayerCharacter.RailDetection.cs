
using Sandbox;
using Sandbox.MovementModes;

public sealed partial class PlayerCharacter : Component
{
	Vector3 lastTracePos;
	float railTraceRadius = 20f;

	public TimeUntil _timeUntilCanGrind = 0;

	void TryGetRail()
	{
		if ( _activeMovementMode == GetMovementMode<RailGrindMovement>() || _timeUntilCanGrind > 0 )
		{
			lastTracePos = WorldPosition;
			return;
		}


			var tr = Scene.Trace.Sphere( railTraceRadius, lastTracePos, WorldPosition )
		.IgnoreGameObject( GameObject )
		.Run();

		bool railCondition = tr.Hit && tr.GameObject.Tags.Contains<String>( "rail" );

		DebugOverlay.Sphere( new Sphere( lastTracePos, railTraceRadius ), (railCondition ? Color.Green : Color.Red) );
		DebugOverlay.Sphere( new Sphere( WorldPosition, railTraceRadius ), (railCondition ? Color.Green : Color.Red) );

		lastTracePos = WorldPosition;

		if ( railCondition )
		{
			var movementMode = GetMovementMode<RailGrindMovement>();
			if ( movementMode != null )
			{
				movementMode.RailGrind = tr.GameObject.GetComponent<RailGrind>();
				SetMovementMode<RailGrindMovement>();
			}
		}
	}
}
