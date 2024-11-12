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
	public GameObject forwardArrow { get; set; }

	[Property]
	public GameObject rightArrow { get; set; }

	[Property]
	public CameraComponent camera { get; set; }

	public Vector3 wishVelocity = Vector3.Zero;

	private int maxBounces = 5;

	private float skinWidth = 0.015f;

	protected override void OnAwake()
	{
		rigid = Components.Get<Rigidbody>();
	}

	protected override void OnFixedUpdate()
	{
		CalculateMovementArrows();
		BuildWishVelocity();
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

		float convertedDot = MapRange( dot, -1, 1, 5, 1 );

		if ( trace.Hit )
		{
			if ( wishVelocity.Length > 0 )
			{
				rigid.ApplyForce( wishVelocity * 4500000 * convertedDot );
			}
			else
			{
				rigid.ApplyForce( rigid.Velocity * -5000 );
			}
		}
		else
		{
			Vector3 preVelocity = rigid.Velocity;
			rigid.ApplyForce( wishVelocity * 4500000 * convertedDot );

		}
	

		if ( rigid.Velocity.Length > 30 )
		{
			if ( trace.Hit )
			{
				Rotation targetRotation = Rotation.LookAt( rigid.Velocity.Normal, trace.Normal );
				WorldRotation = Rotation.Slerp( WorldRotation, targetRotation, 10 * Time.Delta );
			} else
			{
				Rotation targetRotation = Rotation.LookAt( rigid.Velocity.WithZ(0).Normal, new Vector3(0,0,1) );
				WorldRotation = Rotation.Slerp( WorldRotation, targetRotation, 10 * Time.Delta );
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
		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();
		rigid.PhysicsBody.GravityScale = 1.2f;
		if ( Input.Pressed( "Jump" ) && trace.Hit ) rigid.Velocity = rigid.Velocity.WithZ( rigid.Velocity.z + 500 );
	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}
}
