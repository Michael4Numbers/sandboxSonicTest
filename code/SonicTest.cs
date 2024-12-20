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


	protected override void OnAwake()
	{
		base.OnAwake();

		Log.Info("Use animation graph: " +  Model.UseAnimGraph);
		Model.OnGenericEvent = ( a ) =>
		{
			if ( _player.rigid.Velocity.Length > 50 )
			{
				//PlayerController.PlayFootstepSound( WorldPosition, 1.0f, 1 );
				Sound.Play( "footstep-concrete", WorldPosition );
			}
		};

		_player = GetComponentInParent<PlayerCharacter>();
	}

	protected override void OnUpdate()
	{
		Model.Set( "move_speed", _player.rigid.Velocity.Length );
		Model.Set( "fall_speed", _player.rigid.Velocity.z );
		var trace = Scene.Trace.Ray( GameObject.WorldPosition + (GameObject.WorldRotation.Up * 10.0f), GameObject.WorldPosition + (GameObject.WorldRotation.Down * 10.0f) )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player" )
			.Run();
		Log.Info( "Player " + Network.Owner.DisplayName + " " + _player.IsOnStableGround() );
		Model.Set( "isFalling", !(_player.movementMode.GetType() == typeof(GroundMovement)) ); 
		Model.Set( "RunMultiplier", MapRange( _player.rigid.Velocity.Length, 1250, 3000, 1, 3 ).Clamp(1, 3) );

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

	public static float MapRange( float value, float inMin, float inMax, float outMin, float outMax )
	{
		return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
	}

}
