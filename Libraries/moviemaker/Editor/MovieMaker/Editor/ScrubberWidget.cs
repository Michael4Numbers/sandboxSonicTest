﻿namespace Editor.MovieMaker;

/// <summary>
/// A bar with times and notches on it
/// </summary>
public class ScrubberItem : GraphicsItem
{
	public MovieEditor Editor { get; }
	public Session Session { get; }

	public bool IsTop { get; }

	public ScrubberItem( MovieEditor timelineEditor, bool isTop )
	{
		Session = timelineEditor.Session;
		Editor = timelineEditor;
		IsTop = isTop;

		ZIndex = 5000;

		HandlePosition = new Vector2( 0f, IsTop ? 0f : 1f );
	}

	protected override void OnMousePressed( GraphicsMouseEvent e )
	{
		base.OnMousePressed( e );

		Session.SetCurrentPointer( Session.PixelsToTime( e.LocalPosition.x, true ) );
	}

	protected override void OnMouseMove( GraphicsMouseEvent e )
	{
		base.OnMouseMove( e );

		Session.SetCurrentPointer( Session.PixelsToTime( e.LocalPosition.x, true ) );
	}

	protected override void OnPaint()
	{
		var duration = Session.Clip?.Duration ?? 0f;

		Paint.SetBrushAndPen( DopeSheet.Colors.Background );
		Paint.DrawRect( LocalRect );

		// Darker background for the clip duration

		var range = Session.VisibleTimeRange;

		var startX = FromScene( Session.TimeToPixels( 0f ) ).x;
		var endX = FromScene( Session.TimeToPixels( duration ) ).x;

		Paint.SetBrushAndPen( DopeSheet.Colors.ChannelBackground );
		Paint.DrawRect( new Rect( new Vector2( startX, LocalRect.Top ), new Vector2( endX - startX, LocalRect.Height ) ) );

		Paint.Pen = Color.White.WithAlpha( 0.1f );
		Paint.PenSize = 2;

		if ( IsTop )
		{
			Paint.DrawLine( LocalRect.BottomLeft, LocalRect.BottomRight );
		}
		else
		{
			Paint.DrawLine( LocalRect.TopLeft, LocalRect.TopRight );
		}

		Paint.Antialiasing = true;
		Paint.SetFont( "Roboto", 8, 300 );

		foreach ( var (style, interval) in Session.Ticks )
		{
			var height = Height;
			var margin = 2f;

			switch ( style )
			{
				case TickStyle.TimeLabel:
					Paint.SetPen( Theme.Green.WithAlpha( 0.2f ) );
					height -= 12f;
					margin = 10f;
					break;

				case TickStyle.Major:
					Paint.SetPen( Color.White.WithAlpha( 0.1f ) );
					height -= 6f;
					break;

				case TickStyle.Minor:
					Paint.SetPen( Color.White.WithAlpha( 0.1f ) );
					height = 6f;
					break;
			}

			var y = IsTop ? Height - height - margin : margin;

			var t0 = Math.Max( MathF.Floor( range.Min / interval ) * interval, 0f );
			var t1 = t0 + (range.Max - range.Min);

			for ( var t = t0; t <= t1; t += interval )
			{
				var x = FromScene( Session.TimeToPixels( t ) ).x;

				if ( style == TickStyle.TimeLabel )
				{
					var time = Session.PixelsToTime( ToScene( x ).x );

					Paint.SetPen( Theme.Green.WithAlpha( 0.2f ) );
					Paint.DrawText( new Vector2( x + 6, y ), TimeToString( time, interval ) );
				}
				else
				{
					Paint.DrawLine( new Vector2( x, y ), new Vector2( x, y + height ) );
				}
			}
		}
	}

	private static string TimeToString( float time, float interval )
	{
		return TimeSpan.FromSeconds( time + 0.00049f ).ToString( @"mm\:ss\.fff" );
	}

	int lastState;

	[EditorEvent.Frame]
	public void Frame()
	{
		var state = HashCode.Combine( Session.PixelsPerSecond, Session.TimeOffset, Session.CurrentPointer, Session.PreviewPointer, Session.Clip?.Duration );

		if ( state != lastState )
		{
			lastState = state;
			Update();
		}
	}
}

