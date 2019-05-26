using System.Collections.Generic;

namespace Amaze.Solve
{
	public class Pipe
	{
		private int _id;

		private readonly LinkedList<KeyPoint> _passingPoints = new LinkedList<KeyPoint> ();

#if PIPE_MULTI_NODE_MAP
		private readonly Dictionary<KeyPoint, List<LinkedListNode<KeyPoint>>> _pointNodeMaps = new Dictionary<KeyPoint, List<LinkedListNode<KeyPoint>>> ();
#else
		private readonly Dictionary<KeyPoint, LinkedListNode<KeyPoint>> _pointNodeMaps = new Dictionary<KeyPoint, LinkedListNode<KeyPoint>> ();
#endif

		public void RegisterId (int id)
		{
			_id = id;
		}

		public override string ToString ()
		{
			return $"[{_id}: {_passingPoints.First?.Value} -> {_passingPoints.Last?.Value}]";
		}

		public void AddKeyPoint (KeyPoint keyPoint, bool toBack)
		{
			var node = toBack ? _passingPoints.AddLast (keyPoint) : _passingPoints.AddFirst (keyPoint);

#if PIPE_MULTI_NODE_MAP
			if (_pointNodeMaps.ContainsKey (keyPoint)) {
				_pointNodeMaps [keyPoint].Add (node);
			} else {
				var nodes = new List<LinkedListNode<KeyPoint>> { node };
				_pointNodeMaps.Add (keyPoint, nodes);
			}
#else
			if (!_pointNodeMaps.ContainsKey (keyPoint)) {
				_pointNodeMaps.Add (keyPoint, node);
			} else {
				// update only tee point
				if (keyPoint.ConnectType == ConnectType.Tee) {
					_pointNodeMaps [keyPoint] = node;
				}
			}
#endif
		}

		public LinkedListNode<KeyPoint> FrontPoint => _passingPoints.First;
		public LinkedListNode<KeyPoint> BackPoint => _passingPoints.Last;

		public LinkedListNode<KeyPoint> GetPointNode (KeyPoint point, bool endPrioritized)
		{
#if PIPE_MULTI_NODE_MAP
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
#else
			return _pointNodeMaps.ContainsKey (point) ? _pointNodeMaps [point] : null;
#endif
		}

		public bool IsPointPassed (KeyPoint point)
		{
			return _pointNodeMaps.ContainsKey (point);
		}

		private LinkedListNode<KeyPoint> GetNextNode (LinkedListNode<KeyPoint> node, bool toBack)
		{
			return toBack ? node.Next : node.Previous;
		}
	}
}