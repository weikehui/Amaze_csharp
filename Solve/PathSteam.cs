using System;
using System.Collections.Generic;

namespace Amaze.Solve
{
	public class PathSteam
	{
		private PathSteam ()
		{
		}

		public PathSteam (int matrixWidth, int matrixHeight, int pointCount, KeyPoint startPoint)
		{
			_endPoint = startPoint;

			_pointPathMatrix = new Dictionary<PathNode, int>[matrixHeight, matrixWidth];
			_totalPointCount = pointCount;
			_remainingPointCount = pointCount;
		}

		public static PathSteam Clone (PathSteam other)
		{
			var clone = new PathSteam ();
			clone.CloneMember (other);
			return clone;
		}

		private void CloneMember (PathSteam other)
		{
			var cloneNodeMaps = ClonePathNodes (other);
			CloneMatrix (other, cloneNodeMaps);
		}

		public override string ToString ()
		{
			var msg = $"[{_id}]: remaining {_remainingPointCount}, endPoint {_endPoint}\n";

			msg += OutputPathNodes ();
			msg += OutputMatrix ();

			return msg;
		}


		#region Id

		private int _id = -1;

		public void RegisterId (int id)
		{
			_id = id;
		}

		#endregion


		#region Path Node

		private readonly LinkedList<PathNode> _pathNodes = new LinkedList<PathNode> ();

		private readonly Dictionary<Pipe, List<LinkedListNode<PathNode>>> _pipeNodeMaps = new Dictionary<Pipe, List<LinkedListNode<PathNode>>> ();

		private KeyPoint _endPoint;

		public KeyPoint EndPoint => _endPoint;

		public Pipe NextPipe => _endPoint.InPassingPipe;

		public bool IsPipePassed (Pipe pipe)
		{
			return _pipeNodeMaps.ContainsKey (pipe);
		}

		public void AddNode (PathNode pathNode)
		{
			pathNode.StartNode = pathNode.Pipe.GetPointNode (_endPoint, false);

			RegisterNode (pathNode);
			UpdateMatrix (pathNode);

			_endPoint = pathNode.ToBack ? pathNode.Pipe.BackPoint.Value : pathNode.Pipe.FrontPoint.Value;
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

			var newToPathNode = new PathNode (toPathNode.Pipe, !toPathNode.ToBack, false);
			AddNode (newToPathNode);
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

		private Dictionary<PathNode, PathNode> ClonePathNodes (PathSteam other)
		{
			_endPoint = other._endPoint;

			var cloneNodeMaps = new Dictionary<PathNode, PathNode> (other._pathNodes.Count);
			foreach (var otherNode in other._pathNodes) {
				var cloneNode = PathNode.Clone (otherNode);
				RegisterNode (cloneNode);
				cloneNodeMaps.Add (otherNode, cloneNode);
			}

			return cloneNodeMaps;
		}

		private string OutputPathNodes ()
		{
			var msg = $"nodes [{_pathNodes.Count}]:\n";
			foreach (var pathNode in _pathNodes) {
				msg += pathNode + "\n";
			}
			return msg;
		}

		#endregion


		#region Matrix

		//private int[,] _pointMatrix;

		private Dictionary<PathNode, int>[,] _pointPathMatrix;

		private int _totalPointCount;
		private int _remainingPointCount;

		private int PointMatrixWidth => _pointPathMatrix.GetLength (1);
		private int PointMatrixHeight => _pointPathMatrix.GetLength (0);

		private void UpdateMatrix (PathNode pathNode)
		{
			LinkedListNode<KeyPoint> lastKeyPointNode = null;
			pathNode.ForEachPipeAllPointNodes (keyPointNode => {
				if (lastKeyPointNode == null) {
					lastKeyPointNode = keyPointNode;
					return true;
				}

				var deltaX = Math.Sign (keyPointNode.Value.X - lastKeyPointNode.Value.X);
				var deltaY = Math.Sign (keyPointNode.Value.Y - lastKeyPointNode.Value.Y);
				for (int x = lastKeyPointNode.Value.X, y = lastKeyPointNode.Value.Y;
					x != keyPointNode.Value.X || y != keyPointNode.Value.Y;
					x += deltaX, y += deltaY) {
					PassMatrix (pathNode, x, y);
				}

				lastKeyPointNode = keyPointNode;
				return true;
			});

			PassMatrix (pathNode, lastKeyPointNode.Value.X, lastKeyPointNode.Value.Y);
		}

		private void PassMatrix (PathNode pathNode, int x, int y)
		{
			var paths = _pointPathMatrix [y, x];
			if (paths == null) {
				paths = new Dictionary<PathNode, int> { { pathNode, 1 } };
				_pointPathMatrix [y, x] = paths;
				_remainingPointCount--;
				return;
			}

			if (paths.ContainsKey (pathNode)) {
				paths [pathNode]++;
			} else {
				paths.Add (pathNode, 1);
			}
		}

		private void UnPassMatrix (PathNode pathNode, int x, int y)
		{
			var paths = _pointPathMatrix [y, x];
			if (paths == null || !paths.ContainsKey (pathNode)) {
				return;
			}

			if (paths [pathNode] > 1) {
				paths [pathNode]--;
				return;
			}

			paths.Remove (pathNode);

			if (paths.Count <= 0) {
				_pointPathMatrix [y, x] = null;
			}
		}

		private int[,] ExtractPathPassMatrix (PathNode pathNode)
		{
			var width = PointMatrixWidth;
			var height = PointMatrixHeight;
			var pathMatrix = new int[height, width];

			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					var paths = _pointPathMatrix [y, x];
					if (paths == null || !paths.ContainsKey (pathNode)) {
						continue;
					}

					pathMatrix [y, x] = paths [pathNode];
				}
			}

			return pathMatrix;
		}

		private int GetMatrixPassedCount (int x, int y)
		{
			var paths = _pointPathMatrix [y, x];
			if (paths == null) {
				return 0;
			}

			var passedCount = 0;
			foreach (var count in paths.Values) {
				passedCount += count;
			}
			return passedCount;
		}

		private int GetMatrixPassedPathCount (int x, int y)
		{
			var paths = _pointPathMatrix [y, x];
			return paths?.Count ?? 0;
		}

		private void CloneMatrix (PathSteam other, Dictionary<PathNode, PathNode> cloneNodeMaps)
		{
			var width = other.PointMatrixWidth;
			var height = other.PointMatrixHeight;
			_pointPathMatrix = new Dictionary<PathNode, int>[height, width];

			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					var otherPaths = other._pointPathMatrix [y, x];
					if (otherPaths == null) {
						continue;
					}

					var paths = new Dictionary<PathNode, int> ();
					foreach (var otherPath in otherPaths) {
						paths.Add (cloneNodeMaps [otherPath.Key], otherPath.Value);
					}
					_pointPathMatrix [y, x] = paths;
				}
			}

			_totalPointCount = other._totalPointCount;
			_remainingPointCount = other._remainingPointCount;
		}

		public string OutputMatrix ()
		{
			var msg = "matrix:\n";

			var width = PointMatrixWidth;
			var height = PointMatrixHeight;
			for (var y = 0; y < height; y++) {
				for (var x = 0; x < width; x++) {
					msg += GetMatrixPassedCount (x, y) + " ";
				}
				msg += "\n";
			}

			return msg;
		}

		#endregion


		#region Solution

		public bool IsSolved => _remainingPointCount <= 0;

		public bool IsFailed => !IsSolved && _endPoint.ConnectType == ConnectType.End;

		private bool _isTrimmed;

		public void TrimSolutionPaths ()
		{
			if (_isTrimmed) {
				return;
			}

			if (!IsSolved) {
				return;
			}

			foreach (var pathNode in _pathNodes) {
				TrimPathNode (pathNode);
			}

			_isTrimmed = true;
		}

		private void TrimPathNode (PathNode pathNode)
		{
			var passedMatrix = ExtractPathPassMatrix (pathNode);
			var trimmingKeyPointNodes = new List<LinkedListNode<KeyPoint>> ();
			var trimming = true;
			LinkedListNode<KeyPoint> lastKeyPointNode = null;

			pathNode.ForEachPipeReversedPointNodes (keyPointNode => {
				if (!trimming) {
					return false;
				}

				var keyPointPathCount = GetMatrixPassedPathCount (keyPointNode.Value.X, keyPointNode.Value.Y);
				if (keyPointPathCount < 2 && passedMatrix [keyPointNode.Value.Y, keyPointNode.Value.X] < 2) {
					trimming = false;
					return false;
				}

				if (lastKeyPointNode == null) {
					// start point
					lastKeyPointNode = keyPointNode;
					return true;
				}

				if (keyPointNode.Value.ConnectType != ConnectType.Turn && keyPointNode != pathNode.StartNode) {
					// no record if not a turn point
					return true;
				}

				// check points in two key points
				var deltaX = Math.Sign (keyPointNode.Value.X - lastKeyPointNode.Value.X);
				var deltaY = Math.Sign (keyPointNode.Value.Y - lastKeyPointNode.Value.Y);
				for (int x = lastKeyPointNode.Value.X, y = lastKeyPointNode.Value.Y;
					x != keyPointNode.Value.X || y != keyPointNode.Value.Y;
					x += deltaX, y += deltaY) {
					var passedPathCount = GetMatrixPassedPathCount (x, y);
					if (passedPathCount < 2 && passedMatrix [y, x] < 2) {
						trimming = false;
						return false;
					}

					passedMatrix [y, x]--;
				}

				trimmingKeyPointNodes.Add (keyPointNode);
				lastKeyPointNode = keyPointNode;
				return true;
			});

			if (trimming) {
				trimmingKeyPointNodes.Add (pathNode.StartNode);
			}

			if (trimmingKeyPointNodes.Count <= 0) {
				return;
			}

			var reverseKeyPointNode = pathNode.ReverseNode;
			foreach (var trimmingKeyPointNode in trimmingKeyPointNodes) {
				var deltaX = Math.Sign (trimmingKeyPointNode.Value.X - reverseKeyPointNode.Value.X);
				var deltaY = Math.Sign (trimmingKeyPointNode.Value.Y - reverseKeyPointNode.Value.Y);
				for (int x = reverseKeyPointNode.Value.X, y = reverseKeyPointNode.Value.Y;
					x != trimmingKeyPointNode.Value.X || y != trimmingKeyPointNode.Value.Y;
					x += deltaX, y += deltaY) {
					UnPassMatrix (pathNode, x, y);
				}

				reverseKeyPointNode = trimmingKeyPointNode;
			}
			pathNode.ReverseNode = reverseKeyPointNode;
		}

		public int[] OutputSolution ()
		{
			var directions = new List<int> ();

			KeyPoint lastKeyPoint = null;
			var lastDirection = Direction.Unknown;
			var pointMatrix = new int[PointMatrixHeight, PointMatrixWidth];
			var remainingPointCount = _totalPointCount;

			foreach (var pathNode in _pathNodes) {
				pathNode.ForEachPipeAllPointNodesPassReversed (keyPointNode => {
					if (lastKeyPoint == null) {
						lastKeyPoint = keyPointNode.Value;
						return true;
					}

					if (keyPointNode.Value == lastKeyPoint) {
						return true;
					}

					var deltaX = Math.Sign (keyPointNode.Value.X - lastKeyPoint.X);
					var deltaY = Math.Sign (keyPointNode.Value.Y - lastKeyPoint.Y);

					// detect direction change
					var direction = DirectionExtensions.DeltaDirection (deltaX, deltaY);
					if (direction == Direction.Unknown) {
						Console.WriteLine ("-------------------------------");
						Console.WriteLine ($"!!!!!! CAN link key point ({lastKeyPoint} -> {keyPointNode.Value} !!!!!!");
						Console.WriteLine ("-------------------------------");
					}
					if (direction != lastDirection) {
						directions.Add ((int)direction);
					}

					// change remaining ount
					for (int x = lastKeyPoint.X, y = lastKeyPoint.Y; x != keyPointNode.Value.X || y != keyPointNode.Value.Y; x += deltaX, y += deltaY) {
						if (pointMatrix [y, x]++ == 0) {
							if (--remainingPointCount <= 0) {
								return false;
							}
						}
					}
					if (pointMatrix [keyPointNode.Value.Y, keyPointNode.Value.X]++ == 0) {
						if (--remainingPointCount <= 0) {
							return false;
						}
					}

					lastKeyPoint = keyPointNode.Value;
					lastDirection = direction;
					return true;
				});
			}

			return directions.ToArray ();
		}

		#endregion
	}


	public class PathNode
	{
		private int _id;

		public Pipe Pipe;
		public bool ToBack;
		public bool HasBranch;

		public LinkedListNode<KeyPoint> StartNode;
		public LinkedListNode<KeyPoint> ReverseNode;

		private PathNode ()
		{
		}

		// public PathNode (Pipe pipe, bool toBack, bool hasBranch, LinkedListNode<KeyPoint> startNode)
		public PathNode (Pipe pipe, bool toBack, bool hasBranch)
		{
			Pipe = pipe;
			ToBack = toBack;
			HasBranch = hasBranch;

			ReverseNode = toBack ? pipe.FrontPoint : pipe.BackPoint;
		}

		public static PathNode Clone (PathNode other)
		{
			var clone = new PathNode ();
			clone.CloneMember (other);
			return clone;
		}

		public override string ToString ()
		{
			return
				$"{_id}: pipe {Pipe}, start {StartNode.Value}, reverse {ReverseNode.Value}, {(ToBack ? "toBack" : "toFront")}{(HasBranch ? ", branch" : "")}";
		}

		private void CloneMember (PathNode other)
		{
			Pipe = other.Pipe;
			ToBack = other.ToBack;
			HasBranch = other.HasBranch;

			StartNode = other.StartNode;
			ReverseNode = other.ReverseNode;
		}

		public void RegisterId (int id)
		{
			_id = id;
		}

		public void ForEachPipeAllPointNodes (Predicate<LinkedListNode<KeyPoint>> keyPointPredicate)
		{
			if (keyPointPredicate == null) {
				return;
			}

			for (var node = ReverseNode; node != null; node = ToBack ? node.Next : node.Previous) {
				if (!keyPointPredicate (node)) {
					return;
				}
			}
		}

		public void ForEachPipeReversedPointNodes (Predicate<LinkedListNode<KeyPoint>> keyPointPredicate)
		{
			if (keyPointPredicate == null) {
				return;
			}

			for (var node = ReverseNode; node != StartNode; node = ToBack ? node.Next : node.Previous) {
				if (node == null) {
					Console.WriteLine ("-------------------------------");
					Console.WriteLine ($"!!!!!! can NOT reach start node {StartNode} form reverse node {ReverseNode} !!!!!!");
					Console.WriteLine ("-------------------------------");
					return;
				}

				if (!keyPointPredicate (node)) {
					return;
				}
			}

			keyPointPredicate (StartNode);
		}

		public void ForEachPipeAllPointNodesPassReversed (Predicate<LinkedListNode<KeyPoint>> keyPointPredicate)
		{
			if (keyPointPredicate == null) {
				return;
			}

			for (var node = StartNode; node != ReverseNode; node = ToBack ? node.Previous : node.Next) {
				if (node == null) {
					Console.WriteLine ("-------------------------------");
					Console.WriteLine ($"!!!!!! can NOT reach reverse node {ReverseNode} form start node {StartNode} !!!!!!");
					Console.WriteLine ("-------------------------------");
					return;
				}

				if (!keyPointPredicate (node)) {
					return;
				}
			}

			for (var node = ReverseNode; node != null; node = ToBack ? node.Next : node.Previous) {
				if (!keyPointPredicate (node)) {
					return;
				}
			}
		}
	}
}