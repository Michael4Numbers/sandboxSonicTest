﻿namespace Editor.MovieMaker;

#nullable enable

partial class Session
{
	private string CookiePrefix => $"moviemaker.{Player.ReferencedClip?.ResourceId.ToString() ?? Player.Id.ToString()}";

	public T GetCookie<T>( string key, T fallback )
	{
		return ProjectCookie.Get( $"{CookiePrefix}.{key}", fallback );
	}

	public void SetCookie<T>( string key, T value )
	{
		ProjectCookie.Set( $"{CookiePrefix}.{key}", value );
	}

	public class CookieHelper( Session session )
	{
		public EditModeType EditMode
		{
			get => MovieMaker.EditMode.Get( session.GetCookie( nameof(EditMode), "" ) );
			set => session.SetCookie( nameof(EditMode), value.Name );
		}

		public bool FrameSnap
		{
			get => session.GetCookie( nameof(FrameSnap), true );
			set => session.SetCookie( nameof(FrameSnap), value );
		}

		public float TimeOffset
		{
			get => session.GetCookie( nameof(TimeOffset), 0f );
			set => session.SetCookie( nameof(TimeOffset), value );
		}

		public float PixelsPerSecond
		{
			get => session.GetCookie( nameof( PixelsPerSecond ), 100f );
			set => session.SetCookie( nameof( PixelsPerSecond ), value );
		}
	}

	private CookieHelper? _cookieHelper;
	public CookieHelper Cookies => _cookieHelper ??= new CookieHelper( this );

	public void RestoreFromCookies()
	{
		FrameSnap = Cookies.FrameSnap;
		TimeOffset = Cookies.TimeOffset;
		PixelsPerSecond = Cookies.PixelsPerSecond;

		SmoothPan.Target = SmoothPan.Value = TimeOffset;
		SmoothZoom.Target = SmoothZoom.Value = PixelsPerSecond;

		SetEditMode( Cookies.EditMode );
	}
}
