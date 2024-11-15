using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Editor;
using Editor.Inspectors;
using Sandbox.MovementModes;
namespace Sandbox.Editor;


[CustomEditor( typeof( MovementModeManager ) )]
public class MovementModesEditor : ComponentEditorWidget
{
	private GameObject _gameObject;
	private List<IMovementMode> _movementModes;

	public MovementModesEditor( SerializedObject obj ) : base( obj )
	{
		Layout = Layout.Column();
		
		_gameObject = SerializedObject.Targets.OfType<MovementModeManager>().First().GameObject;
		
		
		RebuildUI();
	}


	void RebuildUI()
	{
		Layout.Clear( true );
		
		_movementModes = new List<IMovementMode>();
		foreach ( var mode in _gameObject.GetComponentsInChildren<IMovementMode>() )
		{
			_movementModes.Add( mode );
		}

		foreach ( var mode in _movementModes )
		{
			// Hide the mode component 
			mode.Flags = mode.Flags.WithFlag( ComponentFlags.Hidden, true );
		}
		
		var tabs = new TabWidget( this );

		foreach ( var mode in _movementModes )
		{
			var comp = new ComponentSheet( Guid.Empty, mode.GetSerialized() );
			//comp.Header.Enabled = false;
			comp.Header.Hidden = true;
			tabs.AddPage( comp.Header.Title, "description",  comp);
		}
		
		Layout.Add( tabs );

		var removeCompButton = new Button( "Remove Mode", this );
		removeCompButton.Tint = Color.Red * 0.4f;
		removeCompButton.Clicked += () =>
		{
			var curPage = tabs.CurrentPage as ComponentSheet;
			if ( curPage == null ) return;
			// I LOVE C# REFLECTION!!!!!
			var targetObj = curPage.GetType().GetField( "TargetObject", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance )?.GetValue( curPage ) as SerializedObject;
			if (targetObj == null) return;

			foreach ( var mode in _movementModes )
			{
				if ( mode.GetSerialized().TypeName == targetObj.TypeName )
				{
					_movementModes.Remove( mode );
					mode.Destroy();
					
					RebuildUI();
					return;
				}
			}
		};
		
		var refreshUI = new Button( "Refresh UI", this );
		refreshUI.Tint = Color.Green * 0.4f;
		refreshUI.Clicked += () =>
		{
			RebuildUI();
		};

		var Row = Layout.AddRow();
		Row.Add( removeCompButton );
		Row.Add( refreshUI );
	}
}
