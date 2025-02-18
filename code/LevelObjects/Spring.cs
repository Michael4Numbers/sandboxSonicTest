using Sandbox;
using Sandbox.MovementModes;
using System;

public sealed class Spring : Component, Component.ITriggerListener, Component.ExecuteInEditor
{
	[Property] public float LaunchSpeed = 1500f;
	[Property] public Vector3 finalLocation = new Vector3(1000,0,0);

	// Trajectory flight time
	private float tf = 0;
	
	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Contains<string>( "player" ) )
		{
			//Log.Info( other );
			var player = other.GetComponent<PlayerCharacter>();
			player.WorldPosition = WorldPosition + WorldRotation.Up * 64f;
			player.rigid.Velocity = CalculateLaunchVelocity(player.WorldPosition, player.WorldPosition + Rotation.FromYaw(WorldRotation.Yaw()) * finalLocation, player.Gravity.Length);//WorldRotation.Up * LaunchSpeed);

			player.GetMovementMode<AirMovement>()._timeUntilSpringTrajectoryOver = tf;
			
			player.ClearAirDash();
			player.SetBallMode( 0 );
			Sound.Play( "levelobject_spring", WorldPosition );
			player.SetMovementMode<AirMovement>();
		} 
	}
	
	public Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 end, float gravity)
	{
		tf = (end - start).WithZ( 0 ).Length / LaunchSpeed;

		Vector3 vx = LaunchSpeed * (end - start).WithZ( 0 ).Normal;

		float vy = (end - start).z + 0.5f * gravity * tf * tf;
		return vx + vy * Vector3.Up / tf;
	}
	

	#region EDITOR VIZ
	
	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if ( !Gizmo.IsSelected ) return;

		// World space transform for everything!!!!!
		Gizmo.Transform = global::Transform.Zero;
		
		Vector3 gizmoLoc = WorldPosition + Rotation.FromYaw(WorldRotation.Yaw()) * finalLocation;

		global::Transform gizmoTransform = new Transform( gizmoLoc, Rotation.FromYaw( WorldRotation.Yaw() ) );

		using ( Gizmo.Scope( "SpringEditor", gizmoTransform ) )
		{
			if ( Gizmo.Control.Position( "LaunchPos", 0, out var delta ) )
			{
				if ( Gizmo.IsShiftPressed )
				{
					LaunchSpeed += delta.z;
				}
				else
				{
					finalLocation += delta;
					finalLocation = finalLocation.WithY( 0 );
				}
			}
		}
	}

	protected override void OnPreRender()
	{
		base.OnPreRender();

		if ( Game.IsPlaying ) return;
		
		// Draw trajecotry
		Vector3 pos = WorldPosition;
		Vector3 endPos = pos + Rotation.FromYaw(WorldRotation.Yaw()) * finalLocation;
		Vector3 grav = Scene.PhysicsWorld.Gravity * 3; // Hardcoded gravity scale here because i failed at trying to load and extract the correct value form the player prefab
		Vector3 vel = CalculateLaunchVelocity( pos, endPos, grav.Length );
		
		float dt = 0.01f;
		float tf = (endPos - pos).WithZ( 0 ).Length / LaunchSpeed;

		// This resets any colors and shit we set during this call, otherwise it persists into other gizmo. use cases
		using ( Gizmo.Scope( "Trajectory Viz", global::Transform.Zero ) )
		{
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.SolidSphere( endPos, 25f );

			Gizmo.Draw.Color = Color.Green;
			while ( tf > 0 )
			{
				// euler step
				Vector3 newPos = pos + vel * dt;
				Gizmo.Draw.Line( pos, newPos );
				pos = newPos;
				vel += grav * dt;

				tf -= dt;
			}
		}
	}
	
	#endregion
}
