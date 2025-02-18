﻿using System;

namespace Sandbox.MovieMaker;

#nullable enable

/// <summary>
/// A property somewhere in a scene that is being controlled by a <see cref="MovieTrack"/>.
/// </summary>
public interface IMovieProperty
{
	string PropertyName { get; }
	Type PropertyType { get; }

	bool IsBound { get; }

	/// <summary>
	/// Can this property be controlled by a movie clip?
	/// </summary>
	bool CanWrite { get; }

	object? Value { get; set; }
}

/// <summary>
/// Typed <see cref="IMovieProperty"/>.
/// </summary>
/// <typeparam name="T">Value type stored in the property.</typeparam>
internal interface IMovieProperty<T> : IMovieProperty
{
	new T Value { get; set; }
}

/// <summary>
/// A property referencing a <see cref="GameObject"/> or <see cref="Component"/> in the scene.
/// </summary>
public interface ISceneReferenceMovieProperty : IMovieProperty
{
	GameObject? GameObject { get; }
	Component? Component { get; }
}

/// <summary>
/// Movie property that represents a member inside another property.
/// </summary>
public interface IMemberMovieProperty : IMovieProperty
{
	/// <summary>
	/// Property that this member belongs to.
	/// </summary>
	IMovieProperty TargetProperty { get; }
}

internal static partial class MovieProperty
{
	public static ISceneReferenceMovieProperty FromGameObject( GameObject go )
	{
		return new GameObjectMovieProperty( go );
	}

	public static ISceneReferenceMovieProperty FromGameObject( string placeholderName )
	{
		return new GameObjectMovieProperty( placeholderName );
	}

	public static ISceneReferenceMovieProperty FromGameObject( IMovieProperty target, string placeholderName )
	{
		return new GameObjectMovieProperty( placeholderName );
	}

	public static ISceneReferenceMovieProperty FromComponent( IMovieProperty target, Component comp )
	{
		return new ComponentMovieProperty( target, comp );
	}

	public static ISceneReferenceMovieProperty FromComponentType( IMovieProperty target, Type type )
	{
		return new ComponentMovieProperty( target, type );
	}

	public static IMemberMovieProperty FromMember( IMovieProperty target, string memberName, Type? expectedType )
	{
		if ( IsAnimParam( target, memberName ) )
		{
			return FromAnimParam( target, memberName, expectedType );
		}

		if ( IsMorph( target, memberName ) )
		{
			return FromMorph( target, memberName );
		}

		var targetType = TypeLibrary.GetType( target.PropertyType );
		var member = targetType.Members
			.Where( x => x is FieldDescription or PropertyDescription )
			.FirstOrDefault( m => m.Name == memberName );

		var memberType = member switch
		{
			PropertyDescription propDesc => propDesc.PropertyType,
			FieldDescription fieldDesc => fieldDesc.FieldType,
			_ => throw new ArgumentException(
				$"Unable to find property or field '{memberName}' in type '{targetType.Name}'.", nameof(memberName) )
		};

		var propType = TypeLibrary.GetType( typeof(MemberMovieProperty<>) ).MakeGenericType( [memberType] );

		return TypeLibrary.Create<IMemberMovieProperty>( propType, [target, member] );
	}
}

/// <summary>
/// Movie property that references a <see cref="GameObject"/> in a scene.
/// </summary>
file sealed class GameObjectMovieProperty : IMovieProperty<GameObject?>, ISceneReferenceMovieProperty
{
	private IMovieProperty? _target;
	private string _placeholderName;

	private GameObject? _value;

	public string PropertyName => Value?.Name ?? _placeholderName;
	public Type PropertyType => typeof(GameObject);

	public bool IsBound => Value.IsValid();
	public bool CanWrite => false;

	public GameObject? Value
	{
		get => _value ??= AttemptAutoResolve();
		set => _value = value;
	}

	public GameObjectMovieProperty( GameObject value )
		: this ( value.Name )
	{
		Value = value;
	}

	public GameObjectMovieProperty( string placeholderName )
	{
		_placeholderName = placeholderName;
	}

	public GameObjectMovieProperty( IMovieProperty target, string placeholderName )
	{
		_target = target;
		_placeholderName = placeholderName;
	}

	object? IMovieProperty.Value
	{
		get => Value;
		set => Value = (GameObject?)value;
	}

	GameObject? ISceneReferenceMovieProperty.GameObject => Value;
	Component? ISceneReferenceMovieProperty.Component => null;

	private GameObject? AttemptAutoResolve()
	{
		return _target is not { IsBound: true, Value: GameObject go } || _placeholderName is not { } name
			? null
			: go.Children.FirstOrDefault( x => x.Name == name );
	}
}

/// <summary>
/// Movie property that references a <see cref="Component"/> in a scene.
/// </summary>
/// <typeparam name="T">Component type stored in the property.</typeparam>
file sealed class ComponentMovieProperty : IMovieProperty<Component?>, ISceneReferenceMovieProperty, IMemberMovieProperty
{
	private Component? _value;

	public string PropertyName { get; }
	public Type PropertyType { get; }

	public bool IsBound => Value.IsValid();
	public bool CanWrite => false;

	public Component? Value
	{
		get => _value ??= AttemptAutoResolve();
		set => _value = value;
	}

	public IMovieProperty TargetProperty { get; }

	public ComponentMovieProperty( IMovieProperty targetObjectProperty, Component value )
		: this( targetObjectProperty, value.GetType() )
	{
		Value = value;
	}

	public ComponentMovieProperty( IMovieProperty targetObjectProperty, Type componentType )
	{
		TargetProperty = targetObjectProperty;

		PropertyType = componentType;
		PropertyName = componentType.Name;
	}

	object? IMovieProperty.Value
	{
		get => Value;
		set => Value = (Component?)value;
	}

	GameObject? ISceneReferenceMovieProperty.GameObject => Value?.GameObject;
	Component? ISceneReferenceMovieProperty.Component => Value;

	private Component? AttemptAutoResolve()
	{
		return TargetProperty is not { IsBound: true, Value: GameObject go }
			? null
			: go.Components.Get( PropertyType, FindMode.EverythingInSelf );
	}
}

/// <summary>
/// Movie property that references a field or property contained in another <see cref="IMovieProperty"/>.
/// For example, a property in a <see cref="Component"/>.
/// </summary>
/// <typeparam name="T">Value type stored in the property.</typeparam>
file sealed class MemberMovieProperty<T> : IMovieProperty<T>, IMemberMovieProperty
{
	public IMovieProperty TargetProperty { get; }
	public MemberDescription Member { get; }

	public bool IsBound => TargetProperty.IsBound;
	public bool CanWrite => Member switch
	{
		PropertyDescription propDesc => propDesc.CanWrite,
		FieldDescription fieldDesc => !fieldDesc.IsInitOnly,
		_ => false
	};

	public T Value
	{
		// TODO: we can avoid boxing / reflection here when we're in engine code using System.Linq.Expressions

		get => TargetProperty.Value is { } target ? Member switch
		{
			PropertyDescription propDesc => (T)propDesc.GetValue( target ),
			FieldDescription fieldDesc => (T)fieldDesc.GetValue( target ),
			_ => throw new NotImplementedException()
		} : default!;

		set
		{
			if ( TargetProperty.Value is not { } target )
			{
				return;
			}

			SetInternal( target, value );

			if ( Member.TypeDescription.IsValueType )
			{
				TargetProperty.Value = target;
			}
		}
	}

	private void SetInternal( object target, object? value )
	{
		switch ( Member )
		{
			case PropertyDescription propDesc:
				propDesc.SetValue( target, value );
				return;

			case FieldDescription fieldDesc:
				fieldDesc.SetValue( target, value );
				return;

			default:
				throw new NotImplementedException();
		}
	}

	public MemberMovieProperty( IMovieProperty targetProperty, MemberDescription member )
	{
		TargetProperty = targetProperty;
		Member = member;
	}

	string IMovieProperty.PropertyName => Member.Name;
	Type IMovieProperty.PropertyType => typeof(T);

	object? IMovieProperty.Value
	{
		get => Value;
		set => Value = (T)value!;
	}
}
