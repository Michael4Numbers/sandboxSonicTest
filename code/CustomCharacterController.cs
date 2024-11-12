#region Assembly Sandbox.Game, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.1.1.7464
#endregion

using System;
using Sandbox.Internal;

namespace Sandbox;


[Title( "Custom Character Controller" )]
[Category( "Physics" )]
[Icon( "directions_walk" )]
[EditorHandle( "materials/gizmo/charactercontroller.png" )]

public class CustomCharacterController : Component
{

	private int _stuckTries;

	[SkipHotload]
	private static readonly Attribute[] __Velocity__Attrs = new Attribute[2]
	{
		new SyncAttribute(),
		new SyncAttribute()
	};

	private Vector3 _repback__Velocity;

	[SkipHotload]
	private static readonly Attribute[] __IsOnGround__Attrs = new Attribute[2]
	{
		new SyncAttribute(),
		new SyncAttribute()
	};

	private bool _repback__IsOnGround;

	[Range( 0f, 200f, 0.01f, true, true )]
	[Property]
	[DefaultValue( 16f )]

	public float Radius { get; set; } = 16f;


	[Range( 0f, 200f, 0.01f, true, true )]
	[Property]
	[DefaultValue( 64f )]

	public float Height { get; set; } = 64f;


	[Range( 0f, 50f, 0.01f, true, true )]
	[Property]
	[DefaultValue( 18f )]

	public float StepHeight { get; set; } = 18f;


	[Range( 0f, 90f, 0.01f, true, true )]
	[Property]
	[DefaultValue( 45f )]

	public float GroundAngle { get; set; } = 45f;


	[Range( 0f, 64f, 0.01f, true, true )]
	[Property]
	[DefaultValue( 10f )]

	public float Acceleration { get; set; } = 10f;


	//
	// Summary:
	//     When jumping into walls, should we bounce off or just stop dead?
	[Range( 0f, 1f, 0.01f, true, true )]
	[Property]
	[DefaultValue( 0.3f )]
	[Description( "When jumping into walls, should we bounce off or just stop dead?" )]
	public float Bounciness { get; set; } = 0.3f;


	//
	// Summary:
	//     If enabled, determine what to collide with using current project's collision
	//     rules for the Sandbox.GameObject.Tags of the containing Sandbox.GameObject.
	[Property]
	[Group( "Collision" )]
	[Title( "Use Project Collision Rules" )]
	[DefaultValue( false )]
	[Description( "If enabled, determine what to collide with using current project's collision rules for the <see cref=\"P:Sandbox.GameObject.Tags\" /> of the containing <see cref=\"T:Sandbox.GameObject\" />." )]
	public bool UseCollisionRules { get; set; } = false;


	[Property]
	[Group( "Collision" )]
	[HideIf( "UseCollisionRules", true )]
	public TagSet IgnoreLayers { get; set; } = new TagSet();



	public BBox BoundingBox => new BBox( new Vector3( 0f - Radius, 0f - Radius, 0f ), new Vector3( Radius, Radius, Height ) );

	public Vector3 Velocity;


	public bool IsOnGround;

	public GameObject GroundObject { get; set; }

	public Collider GroundCollider { get; set; }

	protected override void DrawGizmos()
	{
		Gizmo.GizmoDraw draw = Gizmo.Draw;
		BBox box = BoundingBox;
		draw.LineBBox( in box );
	}

	//
	// Summary:
	//     Add acceleration to the current velocity. No need to scale by time delta - it
	//     will be done inside.
	[Description( "Add acceleration to the current velocity.  No need to scale by time delta - it will be done inside." )]
	[SourceLocation( "Scene\\Components\\CharacterController\\CharacterController.cs", 60 )]
	public void Accelerate( Vector3 vector )
	{
		Velocity = Velocity.WithAcceleration( vector, Acceleration * Time.Delta );
	}

	//
	// Summary:
	//     Apply an amount of friction to the current velocity. No need to scale by time
	//     delta - it will be done inside.
	[Description( "Apply an amount of friction to the current velocity. No need to scale by time delta - it will be done inside." )]
	[SourceLocation( "Scene\\Components\\CharacterController\\CharacterController.cs", 69 )]
	public void ApplyFriction( float frictionAmount, float stopSpeed = 140f )
	{
		float length = Velocity.Length;
		if ( !(length < 0.01f) )
		{
			float num = ((length < stopSpeed) ? stopSpeed : length);
			float num2 = num * Time.Delta * frictionAmount;
			float num3 = length - num2;
			if ( num3 < 0f )
			{
				num3 = 0f;
			}

			if ( num3 != length )
			{
				num3 /= length;
				Velocity *= num3;
			}
		}
	}

	[SourceLocation( "Scene\\Components\\CharacterController\\CharacterController.cs", 90 )]
	private SceneTrace BuildTrace( Vector3 from, Vector3 to )
	{
		return BuildTrace( base.Scene.Trace.Ray( in from, in to ) );
	}

	[SourceLocation( "Scene\\Components\\CharacterController\\CharacterController.cs", 92 )]
	private SceneTrace BuildTrace( SceneTrace source )
	{
		BBox hull = BoundingBox;
		SceneTrace sceneTrace = source.Size( in hull ).IgnoreGameObjectHierarchy( base.GameObject );
		return UseCollisionRules ? sceneTrace.WithCollisionRules( base.Tags ) : sceneTrace.WithoutTags( IgnoreLayers );
	}

	//
	// Summary:
	//     Trace the controller's current position to the specified delta
	[Description( "Trace the controller's current position to the specified delta" )]
	[SourceLocation( "Scene\\Components\\CharacterController\\CharacterController.cs", 102 )]
	public SceneTraceResult TraceDirection( Vector3 direction )
	{
		return BuildTrace( WorldPosition, WorldPosition + direction ).Run();
	}

	private void Move( bool step )
	{
		if ( step && IsOnGround )
		{
			Velocity = Velocity.WithZ( 0f );
		}

		if ( Velocity.Length < 0.001f )
		{
			Velocity = Vector3.Zero;
			return;
		}

		Vector3 position = WorldPosition;
		CharacterControllerHelper characterControllerHelper = new CharacterControllerHelper( BuildTrace( position, position ), position, Velocity );
		characterControllerHelper.Bounce = Bounciness;
		characterControllerHelper.MaxStandableAngle = GroundAngle;
		if ( step && IsOnGround )
		{
			characterControllerHelper.TryMoveWithStep( Time.Delta, StepHeight );
		}
		else
		{
			characterControllerHelper.TryMove( Time.Delta );
		}

		WorldPosition = characterControllerHelper.Position;
		Velocity = characterControllerHelper.Velocity;
	}

	private void CategorizePosition()
	{
		Vector3 position = WorldPosition;
		Vector3 to = position + Vector3.Down * 2f;
		Vector3 from = position;
		bool isOnGround = IsOnGround;
		if ( !IsOnGround && Velocity.z > 40f )
		{
			ClearGround();
			return;
		}

		to.z -= (isOnGround ? StepHeight : 0.1f);
		SceneTraceResult sceneTraceResult = BuildTrace( from, to ).Run();
		if ( !sceneTraceResult.Hit || Vector3.GetAngle( in Vector3.Up, in sceneTraceResult.Normal ) > GroundAngle )
		{
			ClearGround();
			return;
		}

		IsOnGround = true;
		GroundObject = sceneTraceResult.GameObject;
		GroundCollider = sceneTraceResult.Shape?.Collider as Collider;
		if ( isOnGround && !sceneTraceResult.StartedSolid && sceneTraceResult.Fraction > 0f && sceneTraceResult.Fraction < 1f )
		{
			WorldPosition = sceneTraceResult.EndPosition;
		}
	}

	//
	// Summary:
	//     Disconnect from ground and punch our velocity. This is useful if you want the
	//     player to jump or something.
	public void Punch( in Vector3 amount )
	{
		ClearGround();
		Velocity += amount;
	}


	private void ClearGround()
	{
		IsOnGround = false;
		GroundObject = null;
		GroundCollider = null;
	}

	//
	// Summary:
	//     Move a character, with this velocity
	[Description( "Move a character, with this velocity" )]

	public void Move()
	{
		if ( !TryUnstuck() )
		{
			if ( IsOnGround )
			{
				Move( step: true );
			}
			else
			{
				Move( step: false );
			}

			CategorizePosition();
		}
	}

	//
	// Summary:
	//     Move from our current position to this target position, but using tracing an
	//     sliding. This is good for different control modes like ladders and stuff.
	[Description( "Move from our current position to this target position, but using tracing an sliding. This is good for different control modes like ladders and stuff." )]
	public void MoveTo( Vector3 targetPosition, bool useStep )
	{
		if ( !TryUnstuck() )
		{
			Vector3 position = WorldPosition;
			Vector3 velocity = targetPosition - position;
			CharacterControllerHelper characterControllerHelper = new CharacterControllerHelper( BuildTrace( position, position ), position, velocity );
			characterControllerHelper.MaxStandableAngle = GroundAngle;
			if ( useStep )
			{
				characterControllerHelper.TryMoveWithStep( 1f, StepHeight );
			}
			else
			{
				characterControllerHelper.TryMove( 1f );
			}

			WorldPosition = characterControllerHelper.Position;
		}
	}

	private bool TryUnstuck()
	{
		if ( !BuildTrace( WorldPosition, WorldPosition ).Run().StartedSolid )
		{
			_stuckTries = 0;
			return false;
		}

		int num = 20;
		for ( int i = 0; i < num; i++ )
		{
			Vector3 vector = WorldPosition + Vector3.Random.Normal * ((float)_stuckTries / 2f);
			if ( i == 0 )
			{
				vector = WorldPosition + Vector3.Up * 2f;
			}

			if ( !BuildTrace( vector, vector ).Run().StartedSolid )
			{
				WorldPosition = vector;
				return false;
			}
		}

		_stuckTries++;
		return true;
	}
}
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'Sandbox.System, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Sandbox.System, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\SteamLibrary\steamapps\common\sbox\bin\managed\Sandbox.System.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'Sandbox.Reflection, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Sandbox.Reflection, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\SteamLibrary\steamapps\common\sbox\bin\managed\Sandbox.Reflection.dll'
------------------
Resolve: 'Sandbox.Engine, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Sandbox.Engine, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\SteamLibrary\steamapps\common\sbox\bin\managed\Sandbox.Engine.dll'
------------------
Resolve: 'Sandbox.Filesystem, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'Sandbox.Filesystem, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Load from: 'F:\SteamLibrary\steamapps\common\sbox\bin\managed\Sandbox.Filesystem.dll'
------------------
Resolve: 'Sandbox.Event, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Sandbox.Event, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Facepunch.ActionGraphs, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Facepunch.ActionGraphs, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Text.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Text.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Text.Json.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'System.Numerics.Vectors, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Numerics.Vectors, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Numerics.Vectors.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'System.Threading.Channels, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Threading.Channels, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Threading.Channels.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.Collections.Immutable, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Net.WebSockets.Client, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebSockets.Client, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Net.WebSockets.Client.dll'
------------------
Resolve: 'System.Net.WebSockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.WebSockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Net.WebSockets.dll'
------------------
Resolve: 'ShimSkiaSharp, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
Could not find by name: 'ShimSkiaSharp, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
------------------
Resolve: 'Svg.Model, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
Could not find by name: 'Svg.Model, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
------------------
Resolve: 'Svg.Custom, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
Could not find by name: 'Svg.Custom, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
------------------
Resolve: 'Svg.Skia, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
Could not find by name: 'Svg.Skia, Version=1.0.0.18, Culture=neutral, PublicKeyToken=dafe96fe6c845a74'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'Topten.RichTextKit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Topten.RichTextKit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Linq.Expressions.dll'
------------------
Resolve: 'System.Runtime.Loader, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.Loader, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Runtime.Loader.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Threading.Tasks.Parallel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Tasks.Parallel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Threading.Tasks.Parallel.dll'
------------------
Resolve: 'MonoMod.Utils, Version=25.0.5.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'MonoMod.Utils, Version=25.0.5.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Net.Http.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Net.Http.Json, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Net.Http.Json.dll'
------------------
Resolve: 'System.Memory, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Memory, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Memory.dll'
------------------
Resolve: 'System.Web.HttpUtility, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Web.HttpUtility, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Web.HttpUtility.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'Sentry, Version=4.6.2.0, Culture=neutral, PublicKeyToken=fba2ec45388e2af0'
Could not find by name: 'Sentry, Version=4.6.2.0, Culture=neutral, PublicKeyToken=fba2ec45388e2af0'
------------------
Resolve: 'System.ComponentModel.EventBasedAsync, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.EventBasedAsync, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.ComponentModel.EventBasedAsync.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\Michael\.nuget\packages\microsoft.netcore.app.ref\7.0.20\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
