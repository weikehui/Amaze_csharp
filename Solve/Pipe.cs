using System.Collections.Generic;

namespace Amaze.Solve
{
	public class Pipe
	{
		private int _id;

		private readonly LinkedList<KeyPoint> _passingPoints = new LinkedList<KeyPoint> ();

		private readonly Dictionary<KeyPoint, List<LinkedListNode<KeyPoint>>> _pointNodeMaps = new Dictionary<KeyPoint, List<LinkedListNode<KeyPoint>>> ();

		public void RegisterId (int id)
		{
			_id = id;
		}

		public int Id => _id;

		public override string ToString ()
		{
			return $"[{_id}: {_passingPoints.First?.Value} -> {_passingPoints.Last?.Value}]";
		}

		public void AddKeyPoint (KeyPoint keyPoint, bool toBack)
		{
			var node = toBack ? _passingPoints.AddLast (keyPoint) : _passingPoints.AddFirst (keyPoint);

			if (_pointNodeMaps.ContainsKey (keyPoint)) {
				_pointNodeMaps [keyPoint].Add (node);
			} else {
				var nodes = new List<LinkedListNode<KeyPoint>> { node };
				_pointNodeMaps.Add (keyPoint, nodes);
			}
		}

		public LinkedListNode<KeyPoint> FrontPoint => _passingPoints.First;
		public LinkedListNode<KeyPoint> BackPoint => _passingPoints.Last;

		public LinkedListNode<KeyPoint> GetPointNode (KeyPoint point, bool endPrioritized)
		{
			if (!_pointNodeMaps.ContainsKey (point)) {
				return null;
			}

			var nodes = _pointNodeMaps [point];
			if (nodes.Count < 2) {
				return nodes [0];
			}

			var node = nodes [0];
			var otherNode = nodes [1];

			if (endPrioritized) {
				return node == _passingPoints.First || node == _passingPoints.Last ? node : otherNode;
			}

			return node != _passingPoints.First && node != _passingPoints.Last ? node : otherNode;
		}

		public bool IsPointPassed (KeyPoint point)
		{
			return _pointNodeMaps.ContainsKey (point);
		}

		// public void ForEachAllKeyPoints (KeyPoint startPoint, KeyPoint reversePoint, bool toBack, Action<KeyPoint> pointAction)
		// {
		// 	if (!_pointNodeMaps.ContainsKey (startPoint)) {
		// 		Debug.Log ($"start point {startPoint} is NOT in pipe {this}.");
		// 		return;
		// 	}
		//
		// 	pointAction?.Invoke (startPoint);
		// 	var startNode = _pointNodeMaps [startPoint];
		// 	LinkedListNode<KeyPoint> node;
		//
		// 	if (reversePoint != startPoint) {
		// 		// to reverse
		// 		node = GetNextNode (startNode, !toBack);
		// 		for (; node != null && node.Value != reversePoint; node = GetNextNode (node, !toBack)) {
		// 			pointAction?.Invoke (node.Value);
		// 		}
		//
		// 		pointAction?.Invoke (reversePoint);
		// 		startNode = node ?? (toBack ? _passingPoints.Last : _passingPoints.First);
		// 	}
		//
		// 	// to end
		// 	node = GetNextNode (startNode, toBack);
		// 	for (; node != null; node = GetNextNode (node, toBack)) {
		// 		pointAction?.Invoke (node.Value);
		// 	}
		// }
		//
		// public void ForEachFromToKeyPoints (KeyPoint fromPoint, KeyPoint toPoint, bool toBack, Action<KeyPoint> pointAction)
		// {
		// 	if (!_pointNodeMaps.ContainsKey (fromPoint)) {
		// 		Debug.Log ($"start point {fromPoint} is NOT in pipe {this}.");
		// 		return;
		// 	}
		//
		// 	var node = _pointNodeMaps [fromPoint];
		// 	for (; node != null && node.Value != toPoint; node = GetNextNode (node, toBack)) {
		// 		pointAction?.Invoke (node.Value);
		// 	}
		//
		// 	if (node != null) {
		// 		pointAction?.Invoke (node.Value);
		// 	}
		// }

		private LinkedListNode<KeyPoint> GetNextNode (LinkedListNode<KeyPoint> node, bool toBack)
		{
			return toBack ? node.Next : node.Previous;
		}
	}
}