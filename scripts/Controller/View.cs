using Godot;
using System;

namespace Controller;

public partial class View : Node3D
{
	private Vector3 cameraPosition;

	private Vector3 cameraRotation;

	[Export]
	private Camera3D camera;
		
	public override void _Ready()
	{
		cameraRotation = RotationDegrees; // Initial rotation
		cameraPosition = Vector3.Zero;
		camera = GetNode<Camera3D>("Camera");
	}

	public override void _Process(double delta)
	{
		// Set position and rotation to targets
		Position = Position.Lerp(cameraPosition, (float)delta * 8);
		RotationDegrees = RotationDegrees.Lerp(cameraRotation, (float)delta * 6);
			
		HandleInput(delta);
	}

	/// <summary>
	/// Handle input
	/// </summary>
	/// <param name="delta"></param>
	private void HandleInput(double delta)
	{
		// Rotation
		var input = Vector3.Zero;

		input.X = Input.GetAxis("camera_left", "camera_right");
		input.Z = Input.GetAxis("camera_forward", "camera_back");

		input = input.Rotated(Vector3.Up, Rotation.Y).Normalized();

		cameraPosition += input / 4;

		// Back to center
		if (Input.IsActionPressed("camera_center"))
		{
			cameraPosition = Vector3.Zero;
		}
	}

	public override void _Input(InputEvent e)
	{
		// Rotate camera using mouse (hold 'middle' mouse button)
		if(e is InputEventMouseMotion && Input.IsActionPressed("camera_rotate"))
		{
			cameraRotation += new Vector3(0, -((InputEventMouseMotion)e).Relative.X / 10, 0);
		}
	}
}
