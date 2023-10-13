using Godot;
using System;
using System.Collections.Generic;
using Data;

namespace Cotnroller;

public partial class Builder : Node3D
{
	[Export] public Structure[] Structures { get; set; }
	[Export] public Node3D Selector { get; set; } // The 'cursor'
	[Export] public Node3D SelectorContainer { get; set; } // Node that holds a preview of the structure 
	[Export] public Camera3D ViewCamera { get; set; } // Used for ray casting mouse
	[Export] public GridMap Gridmap { get; set; }
	[Export] public Label CashDisplay { get; set; }

	private DataMap map;
	private int index = 0; // Index of structure being build
	private Plane plane; // Used for ray casting mouse
		
	public override void _Ready()
	{
		map = new DataMap();
		plane = new Plane(Vector3.Up, Vector3.Zero);
			
		// Create new MeshLibrary dynamically, can also be done in the editor
		// See: https://docs.godotengine.org/en/stable/tutorials/3d/using_gridmaps.html

		var meshLibrary = new MeshLibrary();
		
		foreach (var structure in Structures)
		{
			var id = meshLibrary.GetLastUnusedItemId();
			meshLibrary.CreateItem(id);
			meshLibrary.SetItemMesh(id, GetMesh(structure.Model));
		}
		
		Gridmap.MeshLibrary = meshLibrary;
			
		UpdateStructure();
		UpdateCash();
	}

	public override void _Process(double delta)
	{
		// Controls
		ActionRotate(); // Rotate selection 90 degrees
		ActionStructureToggle(); // Toggles between structures
		ActionSave(); // Saving
		ActionLoad(); // Loading
			
		// Map position based on mouse
		var worldPosition = plane.IntersectsRay(
			ViewCamera.ProjectRayOrigin(GetViewport().GetMousePosition()),
			ViewCamera.ProjectRayNormal(GetViewport().GetMousePosition())
		);

		if (worldPosition.HasValue)
		{
			var gridPosition = new Vector3I((int)Mathf.Round(worldPosition.GetValueOrDefault().X),0, (int)Mathf.Round(worldPosition.GetValueOrDefault().Z));
				
			Selector.Position = Selector.Position.Lerp(gridPosition, (float)delta * 40);
			ActionBuild(gridPosition);
			ActionDemolish(gridPosition);
		}
	}

	/// <summary>
	/// Retrieve the mesh from a PackedScene, used for dynamically creating a MeshLibrary
	/// </summary>
	/// <param name="ps"></param>
	/// <returns></returns>
	private Mesh GetMesh(PackedScene ps)
	{
		var sceneState = ps.GetState();
			
		for (int i = 0; i < sceneState.GetNodeCount(); i++)
		{
			if (sceneState.GetNodeType(i) == nameof(MeshInstance3D))
			{
				for (int j = 0; j < sceneState.GetNodePropertyCount(i); j++)
				{
					var propName = sceneState.GetNodePropertyName(i, j);
					if (propName == "mesh")
					{
						var propValue = sceneState.GetNodePropertyValue(i, j).As<Mesh>();
						return (Mesh)propValue.Duplicate();
					}
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Build (place) a structure
	/// </summary>
	/// <param name="griPosition"></param>
	private void ActionBuild(Vector3I griPosition)
	{
		if (Input.IsActionJustPressed("build"))
		{
			var previousTile = Gridmap.GetCellItem(griPosition);
			Gridmap.SetCellItem(griPosition, index, Gridmap.GetOrthogonalIndexFromBasis(Selector.Basis));

			if (previousTile != index)
			{
				map.Cash -= Structures[index].Price;
				UpdateCash();
			}
		}
	}

	/// <summary>
	/// Demolish (remove) a structure
	/// </summary>
	/// <param name="gridPosition"></param>
	private void ActionDemolish(Vector3I gridPosition)
	{
		if (Input.IsActionJustPressed("demolish"))
		{
			Gridmap.SetCellItem(gridPosition, -1);
		}
	}

	/// <summary>
	/// Rotates the 'cursor' 90 degrees
	/// </summary>
	private void ActionRotate()
	{
		if (Input.IsActionJustPressed("rotate"))
		{
			Selector.RotateY(Mathf.DegToRad(90));
				
		}
	}

	/// <summary>
	/// Toggle between structures to build
	/// </summary>
	private void ActionStructureToggle()
	{
		if (Input.IsActionJustPressed("structure_next"))
		{
			index = Mathf.Wrap(index + 1, 0, Structures.Length);
		}
			
		if (Input.IsActionJustPressed("structure_previous"))
		{
			index = Mathf.Wrap(index - 1, 0, Structures.Length);
		}

		UpdateStructure();
	}

	/// <summary>
	/// Update the structure visual in the 'cursor'
	/// </summary>
	private void UpdateStructure()
	{
		foreach (var child in SelectorContainer.GetChildren())
		{
			SelectorContainer.RemoveChild(child);
		}

		var model = (Node3D)Structures[index].Model.Instantiate();
		SelectorContainer.AddChild(model);
		model.Position = new Vector3(model.Position.Z, model.Position.Y + 0.05f, model.Position.Z);
		// set transparency
		var mi = ((MeshInstance3D)model.GetChild(0));
		mi.Transparency = 0.5f;
	}

	private void UpdateCash()
	{
		CashDisplay.Text = $"${map.Cash}";
	}

	#region # Saving/load
	private void ActionSave()
	{
		if (Input.IsActionJustPressed("save"))
		{
			Godot.GD.Print("Saving map...");
			
			foreach (var cell in Gridmap.GetUsedCells())
			{
				var ds = new DataStructure()
				{
					Position = new Vector2I(cell.X, cell.Z),
					Orientation = Gridmap.GetCellItemOrientation(cell),
					Structure = Gridmap.GetCellItem(cell)
				};
				
				map.Structures.Add(ds);
			}

			var test = ResourceSaver.Save(map, "user://map.res");
		}
			
	}

	private void ActionLoad()
	{
		if (Input.IsActionJustPressed("load"))
		{
			GD.Print("loading map...");
				
			Gridmap.Clear();
			map = (DataMap)ResourceLoader.Load("user://map.res");

			if (map is null)
			{
				map = new DataMap();
			}

			foreach (var cell in map.Structures)
			{
				Gridmap.SetCellItem(new Vector3I(cell.Position.X, 0, cell.Position.Y), cell.Structure, cell.Orientation);
			}
				
			UpdateCash();
		}
	}
	#endregion
	
}
