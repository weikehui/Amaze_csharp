using System.Collections.Generic;

namespace Amaze.Solve
{
	public class Solver
	{
		private const int MAX_SOLVE_COUNT = 10;
		private const int MAX_EXECUTE_COUNT = 10000;

		public Solver (int[,] data)
		{
			_width = data.GetLength (1);
			_height = data.GetLength (0);
			_data = data;

			InitKeyPoints ();
			InitPipes ();
			InitPaths ();
		}

		public override string ToString ()
		{
			return $"size ({_width}, {_height}), point count ({_keyPoints.Count}/{_validPointCount}), pipeCount {_pipes.Count}, startPoint {_startPoint}";
		}


		#region Data

		private readonly int _width;
		private readonly int _height;
		private readonly int[,] _data;

		private (int, int) GetEndPosition (int x, int y, Direction direction)
		{
			switch (direction) {
			case Direction.Right:
				for (; x < _width - 1; x++) {
					if (_data [y, x + 1] == 0) {
						break;
					}
				}
				break;

			case Direction.Left:
				for (; x > 0; x--) {
					if (_data [y, x - 1] == 0) {
						break;
					}
				}
				break;

			case Direction.Up:
				for (; y > 0; y--) {
					if (_data [y - 1, x] == 0) {
						break;
					}
				}
				break;

			case Direction.Down:
				for (; y < _height - 1; y++) {
					if (_data [y + 1, x] == 0) {
						break;
					}
				}
				break;
			}

			return (x, y);
		}

		#endregion


		#region Key Point

		private KeyPoint[,] _keyPointMatrix;
		private HashSet<KeyPoint> _keyPoints;

		private KeyPoint _startPoint;
		private int _validPointCount;

		private void InitKeyPoints ()
		{
			_keyPointMatrix = new KeyPoint[_height, _width];
			_keyPoints = new HashSet<KeyPoint> ();

			InitStartPoint ();
			FindKeyPoints (_startPoint);
		}

		private KeyPoint RegisterKeyPoint (int x, int y)
		{
			var (directions, type) = GetConnects (x, y);
			var keyPoint = new KeyPoint {
				X = x,
				Y = y,
				ConnectDirections = directions,
				ConnectType = type
			};

			_keyPointMatrix [y, x] = keyPoint;
			_keyPoints.Add (keyPoint);

			return keyPoint;
		}

		private KeyPoint GetKeyPoint (int x, int y)
		{
			return _keyPointMatrix [y, x];
		}

		private (DirectionFlags, ConnectType) GetConnects (int x, int y)
		{
			var directions = DirectionFlags.None;
			var count = 0;

			// right
			if (x < _width - 1 && _data [y, x + 1] != 0) {
				directions |= DirectionFlags.Right;
				count++;
			}

			// left
			if (x > 1 && _data [y, x - 1] != 0) {
				directions |= DirectionFlags.Left;
				count++;
			}

			// up
			if (y > 1 && _data [y - 1, x] != 0) {
				directions |= DirectionFlags.Up;
				count++;
			}

			// down
			if (y < _height - 1 && _data [y + 1, x] != 0) {
				directions |= DirectionFlags.Down;
				count++;
			}

			return (directions, (ConnectType)count);
		}

		private void InitStartPoint ()
		{
			_validPointCount = 0;

			for (var x = 0; x < _width; x++) {
				for (var y = _height - 1; y >= 0; y--) {
					if (_data [y, x] == 0) {
						continue;
					}

					_validPointCount++;

					if (_startPoint == null) {
						_startPoint = RegisterKeyPoint (x, y);
					}
				}
			}
		}

		private void FindKeyPoints (KeyPoint startPoint)
		{
			FindKeyPoint (startPoint, Direction.Right);
			FindKeyPoint (startPoint, Direction.Left);
			FindKeyPoint (startPoint, Direction.Up);
			FindKeyPoint (startPoint, Direction.Down);
		}

		private void FindKeyPoint (KeyPoint startPoint, Direction direction)
		{
			if (!startPoint.ContainDirection (direction)) {
				return;
			}

			var (x, y) = GetEndPosition (startPoint.X, startPoint.Y, direction);
			var keyPoint = GetKeyPoint (x, y);
			if (keyPoint != null) {
				return;
			}

			keyPoint = RegisterKeyPoint (x, y);
			FindKeyPoints (keyPoint);
		}

		#endregion


		#region Pipe

		private HashSet<Pipe> _pipes;

		private void InitPipes ()
		{
			_pipes = new HashSet<Pipe> ();

			foreach (var keyPoint in _keyPoints) {
				switch (keyPoint.ConnectType) {
				case ConnectType.End:
					CreatePipeFromEndPoint (keyPoint);
					break;

				case ConnectType.Turn:
					CreatePipeFromTurnPoint (keyPoint);
					break;

				case ConnectType.Tee:
					CreatePassPipeFromTPoint (keyPoint);
					CreateEndPipeFromTPoint (keyPoint);
					break;
				}
			}
		}

		private void RegisterPipe (Pipe pipe)
		{
			pipe.RegisterId (_pipes.Count);
			_pipes.Add (pipe);
		}

		private void CreatePipeFromEndPoint (KeyPoint startPoint)
		{
			if (startPoint.InEndPipe != null) {
				return;
			}

			var direction = startPoint.ConnectDirections.ToId ();
			if (direction == Direction.Unknown) {
				return;
			}

			var pipe = new Pipe ();
			RegisterPipe (pipe);
			pipe.AddKeyPoint (startPoint, true);
			startPoint.InEndPipe = pipe;

			FillPipePoints (pipe, startPoint, direction, true);
		}

		private void CreatePipeFromTurnPoint (KeyPoint startPoint)
		{
			if (startPoint.InPassingPipe != null || startPoint.InEndPipe != null) {
				return;
			}

			var horizontalDirectionFlags = startPoint.ConnectDirections & DirectionFlags.Horizontal;
			var horizontalDirection = horizontalDirectionFlags.ToId ();
			var verticalDirectionFlags = startPoint.ConnectDirections & DirectionFlags.Vertical;
			var verticalDirection = verticalDirectionFlags.ToId ();
			if (horizontalDirection == Direction.Unknown || verticalDirection == Direction.Unknown) {
				return;
			}

			var pipe = new Pipe ();
			RegisterPipe (pipe);
			pipe.AddKeyPoint (startPoint, true);
			startPoint.InPassingPipe = pipe;

			FillPipePoints (pipe, startPoint, horizontalDirection, true);
			if (startPoint != pipe.BackPoint.Value) {
				FillPipePoints (pipe, startPoint, verticalDirection, false);
			}
		}

		private void CreatePassPipeFromTPoint (KeyPoint startPoint)
		{
			if (startPoint.InPassingPipe != null) {
				return;
			}

			// pass pipe
			var passingHorizontal = (startPoint.ConnectDirections & DirectionFlags.Horizontal) == DirectionFlags.Horizontal;
			var passingVertical = (startPoint.ConnectDirections & DirectionFlags.Vertical) == DirectionFlags.Vertical;
			if (passingHorizontal == passingVertical) {
				return;
			}

			var pipe = new Pipe ();
			RegisterPipe (pipe);
			pipe.AddKeyPoint (startPoint, true);
			startPoint.InPassingPipe = pipe;

			FillPipePoints (pipe, startPoint, passingHorizontal ? Direction.Right : Direction.Up, true);
			if (startPoint != pipe.BackPoint.Value) {
				FillPipePoints (pipe, startPoint, passingHorizontal ? Direction.Left : Direction.Down, false);
			}
		}

		private void CreateEndPipeFromTPoint (KeyPoint startPoint)
		{
			if (startPoint.InEndPipe != null) {
				return;
			}

			var horizontal = startPoint.ConnectDirections & DirectionFlags.Horizontal;
			var vertical = startPoint.ConnectDirections & DirectionFlags.Vertical;
			if (horizontal == vertical || horizontal != DirectionFlags.None && vertical != DirectionFlags.None) {
				return;
			}

			var pipe = new Pipe ();
			RegisterPipe (pipe);
			pipe.AddKeyPoint (startPoint, true);
			startPoint.InEndPipe = pipe;

			var direction = horizontal != DirectionFlags.None ? horizontal.ToId () : vertical.ToId ();
			FillPipePoints (pipe, startPoint, direction, true);
		}

		private void FillPipePoints (Pipe pipe, KeyPoint startPoint, Direction startDirection, bool toBack)
		{
			var (endPosX, endPosY) = GetEndPosition (startPoint.X, startPoint.Y, startDirection);
			var (deltaX, deltaY) = startDirection.Delta ();
			for (int x = startPoint.X + deltaX, y = startPoint.Y + deltaY; x != endPosX || y != endPosY; x += deltaX, y += deltaY) {
				var keyPoint = GetKeyPoint (x, y);
				if (keyPoint == null) {
					continue;
				}

				pipe.AddKeyPoint (keyPoint, toBack);
				keyPoint.InPassingPipe = pipe;
			}

			var endPoint = GetKeyPoint (endPosX, endPosY);

			bool ended;
			switch (endPoint.ConnectType) {
			case ConnectType.End:
				endPoint.InEndPipe = pipe;
				ended = true;
				break;

			case ConnectType.Turn:
				endPoint.InPassingPipe = pipe;
				// ended if loop
				ended = pipe.IsPointPassed (endPoint);
				break;

			case ConnectType.Tee:
				endPoint.InEndPipe = pipe;
				ended = true;
				break;

			default:
				ended = true;
				break;
			}
			pipe.AddKeyPoint (endPoint, toBack);

			if (ended) {
				return;
			}

			var otherDirectionFlags = DirectionFlags.None;
			switch (startDirection) {
			case Direction.Right:
			case Direction.Left:
				otherDirectionFlags = DirectionFlags.Vertical;
				break;
			case Direction.Up:

			case Direction.Down:
				otherDirectionFlags = DirectionFlags.Horizontal;
				break;
			}

			otherDirectionFlags &= endPoint.ConnectDirections;
			var otherDirection = otherDirectionFlags.ToId ();
			if (otherDirection == Direction.Unknown) {
				return;
			}

			FillPipePoints (pipe, endPoint, otherDirection, toBack);
		}

		#endregion


		#region Path

		private HashSet<PathSteam> _pathSteams;
		private Stack<PathSteam> _newPathSteams;
		private List<PathSteam> _removedPathSteams;
		private List<PathSteam> _solvedPathSteams;
		private int _nextPathId;

		private void InitPaths ()
		{
			_pathSteams = new HashSet<PathSteam> ();
			_newPathSteams = new Stack<PathSteam> ();
			_removedPathSteams = new List<PathSteam> ();
			_solvedPathSteams = new List<PathSteam> ();
		}

		private void RegisterPathSteam (PathSteam pathSteam)
		{
			pathSteam.RegisterId (_nextPathId++);
			_newPathSteams.Push (pathSteam);
		}

		public void Start ()
		{
			switch (_startPoint.ConnectType) {
			case ConnectType.End:
				StartFromEndPoint ();
				break;
			case ConnectType.Turn:
				StartFromTurnPoint ();
				break;
			default:
				Debug.Log ($"connect type [{_startPoint.ConnectType}] is NOT supported!");
				break;
			}

			NormalizePaths ();

			// output
			foreach (var pathSteam in _pathSteams) {
				Debug.Log (pathSteam);
			}
		}

		private void StartFromEndPoint ()
		{
			var pipe = _startPoint.InEndPipe;
			if (pipe == null) {
				return;
			}

			var toBack = _startPoint == pipe.FrontPoint.Value;
			var pathSteam = new PathSteam (_width, _height, _validPointCount, _startPoint);
			RegisterPathSteam (pathSteam);

			var pathNode = new PathNode (pipe, toBack, false);
			pathSteam.AddNode (pathNode);
		}

		private void StartFromTurnPoint ()
		{
			var pipe = _startPoint.InPassingPipe;
			if (pipe == null) {
				return;
			}

			var pathSteam = new PathSteam (_width, _height, _validPointCount, _startPoint);
			pathSteam.AddNode (new PathNode (pipe, true, true));
			RegisterPathSteam (pathSteam);

			var otherPathSteam = new PathSteam (_width, _height, _validPointCount, _startPoint);
			otherPathSteam.AddNode (new PathNode (pipe, false, true));
			RegisterPathSteam (otherPathSteam);
		}

		public void Tick ()
		{
			foreach (var pathSteam in _pathSteams) {
				PathSteamTick (pathSteam);
			}

			NormalizePaths ();

			// output
			foreach (var pathSteam in _pathSteams) {
				Debug.Log (pathSteam);
			}
		}

		private void NormalizePaths ()
		{
			// remove
			foreach (var pathSteam in _removedPathSteams) {
				_pathSteams.Remove (pathSteam);
			}
			_removedPathSteams.Clear ();

			// new
			// foreach (var pathSteam in _newPathSteams) {
			// 	_pathSteams.Add (pathSteam);
			// }
			// _newPathSteams.Clear ();

			while (_pathSteams.Count < MAX_EXECUTE_COUNT && _newPathSteams.Count > 0) {
				var pathSteam = _newPathSteams.Pop ();
				_pathSteams.Add (pathSteam);
			}
		}

		private void PathSteamTick (PathSteam pathSteam)
		{
			if (pathSteam.IsSolved) {
				_removedPathSteams.Add (pathSteam);
				_solvedPathSteams.Add (pathSteam);
				Debug.Log ($"path SOLVED, pathSteam: {pathSteam}");
				return;
			}

			if (pathSteam.IsFailed) {
				_removedPathSteams.Add (pathSteam);
				Debug.Log ($"remove path because failed, pathSteam: {pathSteam}");
				return;
			}

			var pipe = pathSteam.NextPipe;
			if (pipe == null) {
				_removedPathSteams.Add (pathSteam);
				Debug.Log ($"remove path because NO next pipe, pathSteam: {pathSteam}");
				return;
			}

			if (pathSteam.IsPipePassed (pipe)) {
				var (fromNode, toNode) = pathSteam.GetRepeatableNode (pipe);
				if (fromNode == null || toNode == null) {
					_removedPathSteams.Add (pathSteam);
					Debug.Log ($"remove path because NON-repeatable, pathSteam: {pathSteam}");
				} else {
					pathSteam.AddRepeatableNode (fromNode, toNode);
				}
				return;
			}

			// add new pipe
			var otherPathSteam = PathSteam.Clone (pathSteam);
			RegisterPathSteam (otherPathSteam);

			pathSteam.AddNode (new PathNode (pipe, true, true));
			otherPathSteam.AddNode (new PathNode (pipe, false, true));
		}

		// public bool IsEnded => _pathSteams.Count <= 0;
		public bool IsEnded => _solvedPathSteams.Count >= MAX_SOLVE_COUNT || _pathSteams.Count <= 0;

		public void OptimizeSolutions ()
		{
			foreach (var pathSteam in _solvedPathSteams) {
				pathSteam.TrimSolutionPaths ();
			}
		}

		public void OutputSolutionPathSteams ()
		{
			if (_solvedPathSteams.Count <= 0) {
				Debug.Log ("no solution.");
			}

			foreach (var pathSteam in _solvedPathSteams) {
				Debug.Log (pathSteam);
			}
		}

		public int[] OutputShortestSolution ()
		{
			if (_solvedPathSteams.Count <= 0) {
				return null;
			}

			int[] shortestSolution = null;
			foreach (var pathSteam in _solvedPathSteams) {
				var solution = pathSteam.OutputSolution ();
				if (shortestSolution == null || solution.Length < shortestSolution.Length) {
					shortestSolution = solution;
				}
			}

			return shortestSolution;
		}

		public List<int[]> OutputSolutions ()
		{
			if (_solvedPathSteams.Count <= 0) {
				return null;
			}

			var solutions = new List<int[]> ();
			foreach (var pathSteam in _solvedPathSteams) {
				solutions.Add (pathSteam.OutputSolution ());
			}
			return solutions;
		}

		#endregion
	}
}