using Sandbox.Diagnostics;

namespace Sandbox.LevelPrimitives;

public class GravityVolume : Component, Component.ITriggerListener, IScenePhysicsEvents
{
	public enum VolumeType
	{
		/// <summary>
		/// Box would use the 'up' vector of the game object
		/// </summary>
		Box,
		/// <summary>
		/// Sphere would use the 'center' vector of the game object
		/// </summary>
		Sphere
	}
	
	[Property]
	public VolumeType GravityVolumeType { get; set; }

	private PlayerCharacter _player;
	
	public void OnTriggerEnter( GameObject other )
	{
		if ( _player == null )
		{
			_player = other.GetComponent<PlayerCharacter>();
			_player.TargetGravDir = GetGravityDirection();
			_player.UnGround();
		}
	}

	Vector3 GetGravityDirection()
	{
		switch ( GravityVolumeType )
		{
			case VolumeType.Box:
				return WorldRotation.Up;
			case VolumeType.Sphere:
				return (WorldPosition - _player.WorldPosition).Normal;
		}
		
		Assert.True( true, "wtf how'd we get here?" );
		return Vector3.Down;
	}

	public void PrePhysicsStep()
	{
		// Update in the case of sphere
		if ( _player == null ) return;
		
		_player.TargetGravDir = GetGravityDirection();
	}

	public void OnTriggerExit( GameObject other )
	{
		var isPlayer = other.GetComponent<PlayerCharacter>();
		if ( isPlayer != null )
		{
			_player.TargetGravDir = Vector3.Down;
			_player = null;
		}
	}
}
