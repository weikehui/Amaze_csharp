using System;

namespace Amaze.Solve
{
	public enum Direction
	{
		Unknown = 0,
		Right = 1,
		Left = 2,
		Up = 3,
		Down = 4
	}


	[Flags]
	public enum DirectionFlags
	{
		None = 0x00,
		Right = 0x01 << Direction.Right,
		Left = 0x01 << Direction.Left,
		Up = 0x01 << Direction.Up,
		Down = 0x01 << Direction.Down,

		// combo
		Horizontal = Right | Left,
		Vertical = Up | Down
	}


	public static class DirectionExtensions
	{
		public static DirectionFlags ToFlags (this Direction direction)
		{
			switch (direction) {
			case Direction.Right:
				return DirectionFlags.Right;
			case Direction.Left:
				return DirectionFlags.Left;
			case Direction.Up:
				return DirectionFlags.Up;
			case Direction.Down:
				return DirectionFlags.Down;
			default:
				return DirectionFlags.None;
			}
		}

		public static Direction ToId (this DirectionFlags directionFlags)
		{
			switch (directionFlags) {
			case DirectionFlags.Right:
				return Direction.Right;
			case DirectionFlags.Left:
				return Direction.Left;
			case DirectionFlags.Up:
				return Direction.Up;
			case DirectionFlags.Down:
				return Direction.Down;
			default:
				return Direction.Unknown;
			}
		}

		public static (int, int) Delta (this Direction direction)
		{
			switch (direction) {
			case Direction.Right:
				return (1, 0);
			case Direction.Left:
				return (-1, 0);
			case Direction.Up:
				return (0, -1);
			case Direction.Down:
				return (0, 1);
			default:
				return (0, 0);
			}
		}

		public static Direction Reverse (this Direction direction)
		{
			switch (direction) {
			case Direction.Right:
				return Direction.Left;
			case Direction.Left:
				return Direction.Right;
			case Direction.Up:
				return Direction.Down;
			case Direction.Down:
				return Direction.Up;
			default:
				return Direction.Unknown;
			}
		}

		public static Direction DeltaDirection (int deltaX, int deltaY)
		{
			if (deltaX == 0) {
				if (deltaY > 0) {
					return Direction.Down;
				}
				if (deltaY < 0) {
					return Direction.Up;
				}
			}

			if (deltaY == 0) {
				if (deltaX > 0) {
					return Direction.Right;
				}
				if (deltaX < 0) {
					return Direction.Left;
				}
			}

			return Direction.Unknown;
		}
	}
}