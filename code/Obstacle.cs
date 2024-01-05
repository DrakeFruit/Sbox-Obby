using Sandbox;

public sealed class Obstacle : Component
{
	[Property] bool Rotates { get; set; }
	[Property] Rotation RotateDirection { get; set; }
	protected override void OnFixedUpdate()
	{
		if( Rotates ){
			GameObject.Transform.Rotation *= RotateDirection;
		}
	}
}