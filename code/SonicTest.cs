using Sandbox;
using Sandbox.MovementModes;
using System.Numerics;

public sealed class SonicTest : Component
{
	[Property]
	public SkinnedModelRenderer Model { get; set; }


	private PlayerCharacter _player;

	[Property]
	public SoundPointComponent soundPoint { get; set; }

	public Rotation lastRotation;
	public Rotation deltaRotation;
	public float yawRotSpeed;


	protected override void OnAwake()
	{
		base.OnAwake();

		Model.OnGenericEvent = ( a ) =>
		{
			if ( _player.rigid.Velocity.Length > 50 && Model.Tint == "#FFFFFF" )
			{
				//PlayerController.PlayFootstepSound( WorldPosition, 1.0f, 1 );
				Sound.Play( "footstep-concrete", WorldPosition );
			}
		};

		_player = GetComponentInParent<PlayerCharacter>();
	}

	protected override void OnUpdate()
	{
		deltaRotation = Rotation.Difference(lastRotation, WorldRotation);
		lastRotation = WorldRotation;

		yawRotSpeed = deltaRotation.Yaw() / Time.Delta * 0.4f;

		Model.Set( "move_speed", _player.rigid.Velocity.Length );
		Model.Set( "fall_speed", _player.rigid.Velocity.z );
		Model.Set( "yawRotation", yawRotSpeed );
		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();
		Model.Set( "isFalling", !(_player.movementMode.GetType() == typeof(GroundMovement)) ); 
		Model.Set( "RunMultiplier", MapRange( _player.rigid.Velocity.Length, 50, 3000, 1f, 3 ).Clamp( 0.75f, 3) );
		var movementType = _player.movementMode.GetType();
		int movementMode = movementType == typeof(GroundMovement) || movementType == typeof(AirMovement) || movementType == typeof( HomingAttackMovement ) ? 0 : 1 ;
		Model.Set( "MovementMode", movementMode );

		_player.groundBall.PlaybackRate = MapRange( _player._timeSinceDashing, 0, 2, 1, 4 );
		/* Disabled because player game object already smoothly slerps their rotation?
		if( trace.Hit )
		{
			if ( rigid.Velocity.Length > 30 )
			{
				Vector3 targetVelocity = new Vector3( rigid.Velocity.x, rigid.Velocity.y, 0 );
				Rotation targetRotation = parent.WorldRotation;
				Model.WorldRotation = Rotation.Slerp( Model.WorldRotation, targetRotation, 10 * Time.Delta );
			}
			else
			{
				Vector3 forward = Model.GameObject.WorldRotation.Forward;

				Model.WorldRotation = Rotation.LookAt( forward, new Vector3( 0, 0, 1 ) );
			}
		}
		else
		{
			if ( rigid.Velocity.WithZ(0).Length > 30 )
			{
				Rotation targetRotation = parent.WorldRotation;
				Model.WorldRotation = Rotation.Slerp( Model.WorldRotation, targetRotation, 10 * Time.Delta );
			}
			else
			{
				Vector3 forward = Model.GameObject.WorldRotation.Forward;

				Model.WorldRotation = Rotation.LookAt( forward, new Vector3( 0, 0, 1 ) );
			}
		}
		*/
	}

	protected override void OnFixedUpdate()
	{

	}

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax, bool clamp = true )
	{
		if ( clamp )
		{
			return MathX.Clamp( ((value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin), outMin, outMax );
		}
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}

}
