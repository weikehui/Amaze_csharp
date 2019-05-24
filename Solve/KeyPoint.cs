namespace Amaze.Solve
{
	public class KeyPoint
	{
		public int X;
		public int Y;
		public DirectionFlags ConnectDirections;
		public ConnectType ConnectType;

		public Pipe InPassingPipe;
		public Pipe InEndPipe;

		public override string ToString ()
		{
			return $"({X}, {Y}, {ConnectType})";
		}

		public bool ContainDirection (Direction direction)
		{
			var flags = direction.ToFlags ();
			return (ConnectDirections & flags) != 0;
		}
	}
}