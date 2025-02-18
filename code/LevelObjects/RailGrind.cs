using Sandbox;

public sealed class RailGrind : Component
{
	public Spline spline { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();
		spline = GetComponent<SplineComponent>().Spline;
	}

	protected override void OnUpdate()
	{

	}

	public Transform GetTransformAtDistance( float distance )
	{
		var pointInfo = spline.SampleAtDistance( distance );
		Vector3 localPos = pointInfo.Position;
		Rotation localRot = Rotation.LookAt( pointInfo.Tangent, pointInfo.Up );
		Transform localTransform = new Transform( localPos, localRot );

		// Construct the world transformation matrix manually
		Matrix translationMatrix = Matrix.CreateTranslation( WorldPosition );
		Matrix rotationMatrix = Matrix.CreateRotation( WorldRotation );
		Matrix worldMatrix = translationMatrix * rotationMatrix; // Combine translation & rotation

		// Transform local position to world position
		Vector3 worldPos = worldMatrix.Transform( localPos );

		Vector3 newWorldPos = rotationMatrix.Transform( localPos );

		// Transform local rotation to world rotation
		Rotation worldRot = WorldRotation * localRot;

		return new Transform( newWorldPos + WorldPosition, worldRot );
	}
}
