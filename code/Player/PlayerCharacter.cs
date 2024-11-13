using Sandbox;
using Sandbox.Services;
using System;
using System.Diagnostics;

public sealed class PlayerCharacter : Component, IScenePhysicsEvents
{
	[Property]
	public Rigidbody rigid { get; set; }

	[Property]
	public CapsuleCollider capsuleCollider { get; set; }

	[Property]
	public float speed { get; set; } = 1f;

	[Property]
	public GameObject arrows { get; set; }



	[Property]
	public GameObject ball { get; set; }
	[Property]
	public SkinnedModelRenderer playermodel { get; set; }



	[Property]
	public CameraComponent camera { get; set; }

	public Vector3 wishVelocity = Vector3.Zero;

	private int maxBounces = 5;


	private float skinWidth = 0.015f;

	private float timeSinceLastJump = 0f;


	private bool jumped = false;

	private bool airDashed = false;

	protected override void OnAwake()
	{
		rigid = Components.Get<Rigidbody>();
	}

	void IScenePhysicsEvents.PrePhysicsStep()
	{
		// Calculate velocities (directly set it)
		CalculateMovementArrows();
		BuildWishVelocity();
		var newSpeed = (rigid.Velocity.Length + (speed * 20 * MapRange( rigid.Velocity.Length, 0, 4000, 1, 0 )));
		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();
		if ( trace.Hit )
		{
			if ( wishVelocity.Length > 0 )
			{
				//rigid.Velocity = (wishVelocity * newSpeed);
				var initDirection = rigid.Velocity.Normal;
				var targetDirection = wishVelocity;
				rigid.Velocity = (Vector3.Slerp( initDirection, targetDirection, .2f ) * newSpeed);
			}
			else
			{
				rigid.Velocity = (rigid.Velocity * 0.95f);
			}
		}
		else
		{
			var initDirection = rigid.Velocity.WithZ( 0 ).Normal;
			var targetDirection = wishVelocity;
			
			if(wishVelocity.Length > 0 )
			{
				rigid.Velocity = (Vector3.Slerp( initDirection, targetDirection, .1f ) * rigid.Velocity.WithZ(0).Length).WithZ(rigid.Velocity.z);
			}
			
		}

		
	}

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		// Find ground
		Move();
	}

	protected override void OnPreRender()
	{
		Gizmo.Draw.Arrow( WorldPosition + (WorldRotation.Up * 20), WorldPosition + wishVelocity + ( WorldRotation.Up * 20 ) );
	}

	void CalculateMovementArrows()
	{
		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size(5f)
			.IgnoreGameObjectHierarchy(GameObject)
			.WithoutTags("player")
			.Run();

		Vector3 groundNormal = new Vector3( 0, 0, 1 );
		if ( trace.Hit )
		{
			groundNormal = trace.Normal;
			//Gizmo.Draw.Arrow( trace.StartPosition, trace.HitPosition + (groundNormal * -5f), 12, 5 );
		} else
		{
			//Gizmo.Draw.Arrow( trace.StartPosition, trace.EndPosition, 12, 5 );
		}

		Vector3 up = groundNormal;
		Vector3 forward = camera.GameObject.WorldRotation.Forward;

		forward = forward - Vector3.Dot(forward, up) * up;
		forward = forward.Normal;

		Vector3 right = Vector3.Cross(up, forward);

		Rotation targetRotation = Rotation.LookAt( forward, up );

		arrows.WorldRotation = targetRotation;
	}

	void BuildWishVelocity()
	{
		wishVelocity = 0;

		var rot = arrows.WorldRotation;
		if ( Input.Down( "Forward" ) ) wishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) wishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) wishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) wishVelocity += rot.Right;

		if ( !wishVelocity.IsNearZeroLength ) wishVelocity = wishVelocity.Normal;

		wishVelocity *= speed;
	}

	void Move()
	{
		// Grabs Gravity from scene
		var gravity = Scene.PhysicsWorld.Gravity;
		float dot = Vector3.Dot( wishVelocity, rigid.Velocity.Normal );

		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();

		

		if ( rigid.Velocity.Length > 30 )
		{
			if ( trace.Hit )
			{
				Rotation targetRotation = Rotation.LookAt( rigid.Velocity.Normal, trace.Normal );
				WorldRotation = targetRotation;
				WorldPosition = trace.HitPosition - trace.Normal * 2.5f;
				rigid.PhysicsBody.GravityScale = 0f;
			} else
			{
				rigid.PhysicsBody.GravityScale = 3f;
				Rotation targetRotation = Rotation.LookAt( rigid.Velocity.WithZ(0).Normal, new Vector3(0,0,1) );
				WorldRotation = targetRotation;
			}

		}
		else
		{
			Rotation targetRotation = Rotation.LookAt( WorldRotation.Forward, trace.Normal );
			WorldRotation = Rotation.Slerp( WorldRotation, targetRotation, 10 * Time.Delta );
		}

	}

	protected override void OnUpdate()
	{
		timeSinceLastJump += Time.Delta;
		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();
		if ( Input.Pressed( "Jump" ) && trace.Hit )
		{
			rigid.Velocity = rigid.Velocity.WithZ( rigid.Velocity.z + 1500 );
			Sound.Play( "player_jump", WorldPosition );
			Sound.Play( "player_jumproll", WorldPosition );
			ball.Enabled = true;
			playermodel.Tint = Color.Transparent;
			timeSinceLastJump = 0;
			jumped = true;
		}
		else if ( trace.Hit && timeSinceLastJump > 0.2f )
		{
			ball.Enabled = false;
			playermodel.Tint = Color.White;
			jumped = false;
			airDashed = false;
		}

		if ( Input.Pressed( "attack1" ) && !trace.Hit && !airDashed )
		{
			Sound.Play( "player_airdash", WorldPosition );
			rigid.Velocity = (WorldRotation.Forward * 3000f).WithZ(0);
			airDashed = true;
			ball.Enabled = true;
			playermodel.Tint = Color.Transparent;
		}

	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}
}
