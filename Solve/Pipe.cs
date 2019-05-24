using System;
using System.Collections.Generic;

namespace Amaze.Solve
{
	public class Pipe
	{
		private int _id;

		private readonly LinkedList<KeyPoint> _passingPoints = new LinkedList<KeyPoint> ();

		private readonly Dictionary<KeyPoint, LinkedListNode<KeyPoint>> _inFlowPointNodes = new Dictionary<KeyPoint, LinkedListNode<KeyPoint>> ();

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
			_inFlowPointNodes [keyPoint] = node;
		}

		public KeyPoint FrontPoint => _passingPoints.First?.Value;
		public KeyPoint BackPoint => _passingPoints.Last?.Value;

		public bool IsPointPassed (KeyPoint point)
		{
			return _inFlowPointNodes.ContainsKey (point);
		}

		public void ForEachKeyPoint (KeyPoint startPoint, KeyPoint reversePoint, bool toBack, Action<KeyPoint> pointAction)
		{
			if (!_inFlowPointNodes.ContainsKey (startPoint)) {
				Debug.Log ($"start point {startPoint} is NOT in pipe {this}.");
				return;
			}

			pointAction?.Invoke (startPoint);

			// to reverse
			var node = GetNextNode (_inFlowPointNodes [startPoint], !toBack);
			for (; node != null && node.Value != reversePoint; node = GetNextNode (node, !toBack)) {
				pointAction?.Invoke (node.Value);
			}
			pointAction?.Invoke (reversePoint);

			// to end
			var startNode = node ?? (toBack ? _passingPoints.Last : _passingPoints.First);
			node = GetNextNode (startNode, toBack);
			for (; node != null; node = GetNextNode (node, toBack)) {
				pointAction?.Invoke (node.Value);
			}
		}

		private LinkedListNode<KeyPoint> GetNextNode (LinkedListNode<KeyPoint> node, bool toBack)
		{
			return toBack ? node.Next : node.Previous;
		}
	}
}