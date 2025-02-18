﻿using System.Linq;
using Sandbox.MovieMaker;

namespace Editor.MovieMaker;

#nullable enable

public class TrackWidget : Widget
{
	public TrackListWidget TrackList { get; }
	public MovieTrack MovieTrack { get; }
	internal IMovieProperty? Property { get; }

	public DopeSheetTrack? DopeSheetTrack { get; set; }

	public Layout Buttons { get; }

	RealTimeSince timeSinceInteraction = 1000;

	private bool _wasVisible;

	public TrackWidget( MovieTrack track, TrackListWidget list )
	{
		TrackList = list;
		MovieTrack = track;
		FocusMode = FocusMode.TabOrClickOrWheel;
		VerticalSizeMode = SizeMode.CanGrow;

		Layout = Layout.Row();
		Layout.Spacing = 12f;
		Layout.Margin = 4f;

		Buttons = Layout.AddRow();
		Buttons.Spacing = 2f;
		Buttons.Margin = 2f;

		Property = TrackList.Session.Player.GetOrAutoResolveProperty( MovieTrack );

		// Track might not be mapped to any property in the current scene

		if ( Property is null )
		{
			return;
		}

		Layout.Add( new Label( Property.PropertyName ) );

		if ( Property is ISceneReferenceMovieProperty )
		{
			if ( Property.IsBound && Property.Value is GameObject go && MovieTrack.Parent is not null )
			{
				return;
			}

			// Add control to retarget a scene reference (Component / GameObject)

			var so = Property.GetSerialized();
			var ctrl = ControlWidget.Create( so.GetProperty( nameof( IMovieProperty.Value ) ) );

			if ( ctrl.IsValid() )
			{
				ctrl.MaximumWidth = 300;
				Layout.Add( ctrl );
			}
		}
	}

	protected override void OnMoved()
	{
		UpdateChannelPosition();
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		DopeSheetTrack?.Destroy();
		DopeSheetTrack = default;
	}

	protected override void OnMouseEnter()
	{
		base.OnMouseEnter();

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnMouseLeave()
	{
		base.OnMouseLeave();

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnBlur( FocusChangeReason reason )
	{
		base.OnBlur( reason );

		if ( Parent is TrackGroup ) Parent.Update();
	}

	protected override void OnDoubleClick( MouseEvent e )
	{
		InspectProperty();
	}

	public void InspectProperty()
	{
		if ( Property is not { } property ) return;
		if ( property.GetTargetGameObject() is not { } go ) return;

		SceneEditorSession.Active.Selection.Clear();
		SceneEditorSession.Active.Selection.Add( go );

		if ( MovieTrack.Parent?.PropertyType == typeof(GameObject) )
		{
			switch ( property.PropertyName )
			{
				case nameof( GameObject.LocalPosition ):
					EditorToolManager.SetSubTool( nameof( PositionEditorTool ) );
					break;

				case nameof( GameObject.LocalRotation ):
					EditorToolManager.SetSubTool( nameof( RotationEditorTool ) );
					break;

				case nameof( GameObject.LocalScale ):
					EditorToolManager.SetSubTool( nameof( ScaleEditorTool ) );
					break;
			}
		}
	}

	protected override Vector2 SizeHint()
	{
		return 32;
	}

	public Color BackgroundColor
	{
		get
		{
			var defaultColor = DopeSheet.Colors.ChannelBackground;
			var hoveredColor = DopeSheet.Colors.ChannelBackground.Lighten( 0.1f );
			var selectedColor = Color.Lerp( defaultColor, Theme.Primary, 0.5f );

			var isHovered = IsUnderMouse;
			var isSelected = IsFocused || menu.IsValid() && menu.Visible;

			return isSelected ? selectedColor
				: isHovered ? hoveredColor
					: defaultColor;
		}
	}

	protected override void OnPaint()
	{
		Paint.Antialiasing = false;
		Paint.SetBrushAndPen( BackgroundColor );
		Paint.DrawRect( new Rect( LocalRect.Left, LocalRect.Top, LocalRect.Width, LocalRect.Height ), 4 );

		if ( !_wasVisible )
		{
			// TODO: I don't know why this is needed, fixes Visible not being true when first positioning channel

			_wasVisible = true;
			UpdateChannelPosition();
		}

		if ( timeSinceInteraction < 2.0f )
		{
			var delta = timeSinceInteraction.Relative.Remap( 2.0f, 0, 0, 1 );
			Paint.SetBrush( Theme.Yellow.WithAlpha( delta ) );
			Paint.DrawRect( new Rect( LocalRect.Right - 4, LocalRect.Top, 32, LocalRect.Height ) );
			Update();
			return;
		}
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		UpdateChannelPosition();
	}

	public void UpdateChannelPosition()
	{
		if ( DopeSheetTrack is null ) return;

		var pos = DopeSheetTrack.GraphicsView.FromScreen( ScreenPosition );

		DopeSheetTrack.DoLayout( new Rect( pos, Size ) );
	}

	Menu menu;

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		menu = new Menu( this );
		menu.AddOption( "Delete", "delete", RemoveTrack );

		if ( MovieTrack.Children.Count > 0 )
		{
			menu.AddOption( "Delete Empty Children", "cleaning_services", RemoveEmptyChildren );
		}

		menu.OpenAtCursor();
	}

	void RemoveTrack()
	{
		MovieTrack.Remove();
		TrackList.RebuildTracksIfNeeded();

		Session.Current?.ClipModified();
	}

	static bool RemoveEmptyChildTracks( MovieTrack track )
	{
		var changed = false;

		foreach ( var child in track.Children.ToArray() )
		{
			changed |= RemoveEmptyChildTracks( child );

			if ( child.Children.Count == 0 && child.Blocks.Count == 0 )
			{
				child.Remove();
				changed = true;
			}
		}

		return changed;
	}

	void RemoveEmptyChildren()
	{
		if ( RemoveEmptyChildTracks( MovieTrack ) )
		{
			TrackList.RebuildTracksIfNeeded();

			Session.Current?.ClipModified();
		}
	}

	public void NoteInteraction()
	{
		timeSinceInteraction = 0;
		Update();
	}
}
