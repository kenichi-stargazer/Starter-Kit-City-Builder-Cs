using Godot;
using System;

namespace Data;

public partial class Structure : Resource
{
	[ExportSubgroup("Model"), Export()]
	public PackedScene Model { get; set; }
		
	[Export, ExportSubgroup("Gameplay")]
	public int Price { get; set; }
}
