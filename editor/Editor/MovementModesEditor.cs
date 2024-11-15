using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Editor;
using Editor.Inspectors;
using Sandbox.MovementModes;
namespace Sandbox.Editor;

[CustomEditor( typeof( MovementModeManager ) )]
public class MovementModesEditor : ComponentEditorWidget
{

	private List<IMovementMode> _movementModes;

	public MovementModesEditor( SerializedObject obj ) : base( obj )
	{
		Layout = Layout.Column();
		
		var go = SerializedObject.Targets.OfType<MovementModeManager>().First().GameObject;
		_movementModes = new List<IMovementMode>();
		foreach ( var mode in go.GetComponentsInChildren<IMovementMode>() )
		{
			_movementModes.Add( mode );
			Log.Info(mode.ToString()  );
		}
		
		
		RebuildUI();
		
	}


	void RebuildUI()
	{
		Layout.Clear( true );

		var tabs = new TabWidget( this );

		foreach ( var mode in _movementModes )
		{
			var comp = new ComponentSheet( Guid.Empty, mode.GetSerialized() );
			//comp.Header.Enabled = false;
			comp.Header.Hidden = true;
			tabs.AddPage( comp.Header.Title, "description",  comp);
		}
		
		Layout.Add( tabs );

	}

}

