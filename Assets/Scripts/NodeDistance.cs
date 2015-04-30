using System.Collections;

public class NodeDistance {
	public float cost { set; get; }
	public Pos source { set; get; }
	public bool visited { set; get; }

	public NodeDistance (float cost, Pos source) {
		this.cost = cost;
		this.source = source;
	}

	public NodeDistance (float cost, Pos source, bool visited) : this(cost, source) {
		this.visited = visited;
	}
}
