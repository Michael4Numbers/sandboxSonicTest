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

	public Transform GetTransformAtDistance( float distance, float speed )
	{
		var pointInfo = spline.SampleAtDistance( distance );

		Rotation finalRot = Rotation.LookAt( speed >= 0 ? pointInfo.Tangent : pointInfo.Tangent * -1, pointInfo.Up );
		Transform local = new Transform( pointInfo.Position, finalRot );

		return Transform.Local.ToWorld( local );
	}
}
