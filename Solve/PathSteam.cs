using System;
using System.Collections.Generic;

namespace Amaze.Solve
{
	public class PathSteam
	{
		private int _id = -1;

		public int Id => _id;

		private readonly LinkedList<PathNode> _pathNodes = new LinkedList<PathNode> ();

		private readonly Dictionary<Pipe, List<LinkedListNode<PathNode>>> _pipeNodeMaps = new Dictionary<Pipe, List<LinkedListNode<PathNode>>> ();

		private KeyPoint _lastEndPoint;

		private int[,] _pointMatrix;

		private int _remainingPointCount;

		private PathSteam ()
		{
		}

		public PathSteam (KeyPoint startPoint, int matrixWidth, int matrixHeight, int pointCount)
		{
			_lastEndPoint = startPoint;
			_pointMatrix = new int[matrixHeight, matrixWidth];
			_remainingPointCount = pointCount;
		}

		public override string ToString ()
		{
			var msg = $"[{_id}]: remaining {_remainingPointCount}, endPoint {_lastEndPoint}, nodes [{_pathNodes.Count}]:\n";
			foreach (var pathNode in _pathNodes) {
				msg += pathNode + "\n";
			}

			msg += OutputMatrix ();

			return msg;
		}

		public void RegisterId (int id)
		{
			_id = id;
		}

		public static PathSteam Clone (PathSteam other)
		{
			var clone = new PathSteam ();
			clone.CloneMember (other);
			return clone;
		}

		private void CloneMember (PathSteam other)
		{
			foreach (var otherNode in other._pathNodes) {
				var cloneNode = PathNode.Clone (otherNode);
				RegisterNode (cloneNode);
			}

			_lastEndPoint = other._lastEndPoint;

			// matrix
			var matrixWidth = other._pointMatrix.GetLength (1);
			var matrixHeight = other._pointMatrix.GetLength (0);
			_pointMatrix = new int[matrixHeight, matrixWidth];
			Array.Copy (other._pointMatrix, _pointMatrix, matrixWidth * matrixHeight);

			_remainingPointCount = other._remainingPointCount;
		}

		public void AddNode (PathNode pathNode)
		{
			RegisterNode (pathNode);
			UpdateMatrix (pathNode);

			_lastEndPoint = pathNode.ToBack ? pathNode.Pipe.BackPoint : pathNode.Pipe.FrontPoint;
		}

		public bool IsPipePassed (Pipe pipe)
		{
			return _pipeNodeMaps.ContainsKey (pipe);
		}

		public (LinkedListNode<PathNode>, LinkedListNode<PathNode>) GetRepeatableNode (Pipe pipe)
		{
			var nodes = _pipeNodeMaps [pipe];
			foreach (var linkedListNode in nodes) {
				if (linkedListNode.Value.HasBranch) {
					return (linkedListNode, linkedListNode);
				}
			}

			// find node from pipe node to last
			foreach (var linkedListNode in nodes) {
				for (var node = linkedListNode.Next; node != null; node = node.Next) {
					if (node.Value.HasBranch) {
						return (linkedListNode, node);
					}
				}
			}

			return (null, null);
		}

		public void AddRepeatableNode (LinkedListNode<PathNode> fromNode, LinkedListNode<PathNode> toNode)
		{
			for (var node = fromNode; node != null && node != toNode; node = node.Next) {
				var clonePathNode = PathNode.Clone (node.Value);
				AddNode (clonePathNode);
			}

			var toPathNode = toNode.Value;
			toPathNode.HasBranch = false;

			var cloneToPathNode = PathNode.Clone (toPathNode);
			cloneToPathNode.ToBack = !toPathNode.ToBack;
			cloneToPathNode.ReversePoint = cloneToPathNode.ToBack ? cloneToPathNode.Pipe.FrontPoint : cloneToPathNode.Pipe.BackPoint;
			AddNode (cloneToPathNode);
		}

		private void RegisterNode (PathNode pathNode)
		{
			pathNode.RegisterId (_pathNodes.Count);
			var linkedListNode = _pathNodes.AddLast (pathNode);

			if (_pipeNodeMaps.ContainsKey (pathNode.Pipe)) {
				_pipeNodeMaps [pathNode.Pipe].Add (linkedListNode);
			} else {
				var cloneNodes = new List<LinkedListNode<PathNode>> { linkedListNode };
				_pipeNodeMaps.Add (pathNode.Pipe, cloneNodes);
			}
		}

		private void UpdateMatrix (PathNode pathNode)
		{
			KeyPoint lastKeyPoint = null;
			int lastDeltaX = 0, lastDeltaY = 0;

			pathNode.ForEachPipeAllKeyPoints (keyPoint => {
				if (lastKeyPoint == null) {
					// first point
					lastKeyPoint = keyPoint;
					return;
				}

				var deltaX = Math.Sign (keyPoint.X - lastKeyPoint.X);
				var deltaY = Math.Sign (keyPoint.Y - lastKeyPoint.Y);
				for (int x = lastKeyPoint.X, y = lastKeyPoint.Y; x != keyPoint.X || y != keyPoint.Y; x += deltaX, y += deltaY) {
					if (_pointMatrix [y, x]++ == 0) {
						_remainingPointCount--;
					}
				}

				if (deltaX != lastDeltaX || deltaY != lastDeltaY) {
					if (lastDeltaX != 0 || lastDeltaY != 0) {
						// turn direction
						if (_pointMatrix [lastKeyPoint.Y, lastKeyPoint.X]++ == 0) {
							_remainingPointCount--;
						}
					}

					lastDeltaX = deltaX;
					lastDeltaY = deltaY;
				}

				lastKeyPoint = keyPoint;
			});

			if (_pointMatrix [lastKeyPoint.Y, lastKeyPoint.X]++ == 0) {
				_remainingPointCount--;
			}
		}

		public bool IsSolved => _remainingPointCount <= 0;

		public bool IsFailed => !IsSolved && _lastEndPoint.ConnectType == ConnectType.End;

		public Pipe NextPipe => _lastEndPoint.InPassingPipe;

		public KeyPoint LastEndPoint => _lastEndPoint;

		public string OutputMatrix ()
		{
			var msg = "matrix:\n";

			var width = _pointMatrix.GetLength (1);
			var height = _pointMatrix.GetLength (0);
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					msg += _pointMatrix [y, x] + " ";
				}
				msg += "\n";
			}

			return msg;
		}

		public void TrimSolutionPaths ()
		{
			if (!IsSolved) {
				return;
			}

			foreach (var pathNode in _pathNodes) {
				TrimPathNode (pathNode);
			}
		}

		private void TrimPathNode (PathNode pathNode)
		{
			// find trimming points
			var trimmingPoints = new List<KeyPoint> ();
			var trimming = true;
			KeyPoint lastPoint = null;
			pathNode.ForEachPipeReversedKeyPoints (keyPoint => {
				if (!trimming) {
					return;
				}

				if (_pointMatrix [keyPoint.Y, keyPoint.X] <= 2) {
					trimming = false;
					return;
				}

				if (lastPoint == null) {
					lastPoint = keyPoint;
					return;
				}

				if (keyPoint.ConnectType != ConnectType.Turn && keyPoint != pathNode.StartPoint) {
					return;
				}

				// check points in two key points
				var deltaX = Math.Sign (keyPoint.X - lastPoint.X);
				var deltaY = Math.Sign (keyPoint.Y - lastPoint.Y);
				for (int x = lastPoint.X + deltaX, y = lastPoint.Y + deltaY; x != keyPoint.X || y != keyPoint.Y; x += deltaX, y += deltaY) {
					if (_pointMatrix [y, x] <= 2) {
						trimming = false;
						return;
					}
				}

				trimmingPoints.Add (keyPoint);
				lastPoint = keyPoint;
			});

			if (trimmingPoints.Count <= 0) {
				return;
			}

			lastPoint = pathNode.ReversePoint;
			foreach (var trimmingPoint in trimmingPoints) {
				var deltaX = Math.Sign (trimmingPoint.X - lastPoint.X);
				var deltaY = Math.Sign (trimmingPoint.Y - lastPoint.Y);
				for (int x = lastPoint.X, y = lastPoint.Y; x != trimmingPoint.X || y != trimmingPoint.Y; x += deltaX, y += deltaY) {
					_pointMatrix [y, x] -= 2;
				}
				_pointMatrix [trimmingPoint.Y, trimmingPoint.X] -= 2;
				lastPoint = trimmingPoint;
			}
			pathNode.ReversePoint = lastPoint;
		}

		public int[] OutputSolution ()
		{
			var directions = new List<int> ();

			KeyPoint lastKeyPoint = null;
			var lastDirection = Direction.Unknown;

			foreach (var pathNode in _pathNodes) {
				pathNode.ForEachPipeAllKeyPoints (keyPoint => {
					if (keyPoint == lastKeyPoint) {
						return;
					}

					if (lastKeyPoint == null) {
						lastKeyPoint = keyPoint;
						return;
					}

					var direction = DirectionExtensions.DeltaDirection (keyPoint.X - lastKeyPoint.X, keyPoint.Y - lastKeyPoint.Y);
					lastKeyPoint = keyPoint;

					if (direction == lastDirection) {
						return;
					}

					directions.Add ((int)direction);
					lastDirection = direction;
				});
			}

			return directions.ToArray ();
		}
	}


	public class PathNode
	{
		private int _id;

		public Pipe Pipe;
		public bool ToBack;
		public bool HasBranch;

		public KeyPoint StartPoint;
		public KeyPoint ReversePoint;

		private PathNode ()
		{
		}

		public PathNode (Pipe pipe, bool toBack, bool hasBranch, KeyPoint startPoint)
		{
			Pipe = pipe;
			ToBack = toBack;
			HasBranch = hasBranch;

			StartPoint = startPoint;
			ReversePoint = toBack ? pipe.FrontPoint : pipe.BackPoint;
		}

		public static PathNode Clone (PathNode other)
		{
			var clone = new PathNode ();
			clone.CloneMember (other);
			return clone;
		}

		public override string ToString ()
		{
			return $"{_id}: pipe {Pipe}, {(ToBack ? "toBack" : "toFront")}{(HasBranch ? ", branch" : "")}";
		}

		private void CloneMember (PathNode other)
		{
			_id = other._id;

			Pipe = other.Pipe;
			ToBack = other.ToBack;
			HasBranch = other.HasBranch;

			StartPoint = other.StartPoint;
			ReversePoint = other.ReversePoint;
		}

		public void RegisterId (int id)
		{
			_id = id;
		}

		public void ForEachPipeAllKeyPoints (Action<KeyPoint> keyPointAction)
		{
			Pipe.ForEachAllKeyPoints (StartPoint, ReversePoint, ToBack, keyPointAction);
		}

		public void ForEachPipeReversedKeyPoints (Action<KeyPoint> keyPointAction)
		{
			Pipe.ForEachFromToKeyPoints (ReversePoint, StartPoint, ToBack, keyPointAction);
		}
	}
}