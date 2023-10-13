using Godot;
using Godot.Collections;

namespace Data;

public partial class DataMap : Resource
{
	[Export] public int Cash { get; set; } = 10000;

	[Export] public Array<DataStructure> Structures { get; set; } = new Array<DataStructure>();
}
