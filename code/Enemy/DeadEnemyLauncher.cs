using Sandbox;
using System;

public sealed class DeadEnemyLauncher : Component, Component.ICollisionListener
{
	[Property] public Vector3 launchDirection;
	[Property] public float launchSpeed;
	[Property] GameObject prtcl;

	bool hasExploded;

	Rigidbody rb;

	protected override void OnAwake()
	{
		base.OnAwake();
		rb = GetComponent<Rigidbody>();
	}

	protected override void OnStart()
	{
		base.OnStart();
		rb.Velocity = launchDirection * launchSpeed;	
		rb.AngularVelocity = Vector3.Random * 25;
	}

	protected override void OnFixedUpdate()
	{
		var tr = Scene.Trace
		.Sphere( 24.0f, WorldPosition, WorldPosition ) // 24 is the radius
		.WithoutTags( "player" ) // ignore GameObjects with this tag
		.Run();

		if ( tr.Hit && !hasExploded )
		{
			Sound.Play( "explosion", WorldPosition );
			var explosion = prtcl.Clone( WorldPosition );
			hasExploded = true;
			DestroyGameObject();
		}
	}


}
