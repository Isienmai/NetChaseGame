using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
	//store the kind of motion each connection requires to be traversed
	public enum CONNECTION_TYPE
	{
		HIGH_LEVEL,
		WALK,
		JUMP,
		FALL,
		NONE
	}
	
	//store the destination, cost, and traversal motion
	public class Connection
	{
		public NodeIndex destination { get; protected set; }
		public float traversalCost { get; protected set; }
		public CONNECTION_TYPE connType { get; protected set; }

		public Connection(NodeIndex dest, float cost, CONNECTION_TYPE connectionType)
		{
			destination = new NodeIndex(dest);
			traversalCost = cost;
			connType = connectionType;
		}
	}
}
