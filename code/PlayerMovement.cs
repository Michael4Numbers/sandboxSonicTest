using Sandbox;
using System;
using System.Diagnostics;

public sealed class PlayerMovement : Component
{

	[Property] public float groundControl { get; set; } = 4.0f;
	[Property] public float airControl { get; set; } = 0.5f;
	[Property] public float maxForce { get; set; } = 50.0f;
	[Property] public float speed { get; set; } = 160.0f;
	[Property] public float runSpeed { get; set; } = 290.0f;
	[Property] public float crouchSpeed { get; set; } = 90.0f;
	[Property] public float jumpForce { get; set; } = 290.0f;

	[Property] public GameObject Head { get; set; }

	public Vector3 wishVelocity = Vector3.Zero;
	public bool isCrouching = false;
	public bool isSprinting = false;
	[Property] public CharacterController characterController;
	private Rigidbody rigidbody;

	protected override void OnAwake()
	{
		//characterController = Components.Get<CharacterController>();
		rigidbody = Components.Get<Rigidbody>();
	}

	protected override void OnUpdate()
	{

		isCrouching = Input.Down( "Crouch" );
		isSprinting = Input.Down( "Run" );

		if ( Input.Pressed( "Jump" ) ) Jump();

		
	}

	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		Move();
	}

	void BuildWishVelocity()
	{
		wishVelocity = 0;

		var rot = Head.WorldRotation;
		if ( Input.Down( "Forward" ) ) wishVelocity += rot.Forward;
		if ( Input.Down( "Backward" ) ) wishVelocity += rot.Backward;
		if ( Input.Down( "Left" ) ) wishVelocity += rot.Left;
		if ( Input.Down( "Right" ) ) wishVelocity += rot.Right;

		wishVelocity = wishVelocity.WithZ( 0 );
		if ( !wishVelocity.IsNearZeroLength ) wishVelocity = wishVelocity.Normal;

		if ( isCrouching ) wishVelocity *= crouchSpeed;
		else if ( isSprinting ) wishVelocity *= runSpeed;
		else wishVelocity *= speed;
	}

	void Move()
	{
		// Grabs Gravity from scene
		var gravity = Scene.PhysicsWorld.Gravity;
		rigidbody.ApplyForce( wishVelocity * 10000 );
		if ( characterController.IsOnGround )
		{
			// Apply Friction/Acceleration
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate( wishVelocity );
			characterController.ApplyFriction( groundControl );

			//rigidbody.Velocity = rigidbody.Velocity.WithZ( 0 );
			
		}
		else
		{
			// Apply air control / Gravity
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate( wishVelocity.ClampLength( maxForce ) );
			characterController.ApplyFriction( airControl );
		}

		// Move the character controller
		characterController.Move();

		// Apply the second half of gravity after movement;
		if ( !characterController.IsOnGround )
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		} else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	void Jump()
	{
		if ( !characterController.IsOnGround ) return;

		characterController.Punch( Vector3.Up * jumpForce );

	}
}
