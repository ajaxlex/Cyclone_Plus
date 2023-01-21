using System;
using System.Collections.Generic;

namespace VertexHerder.Cyclone.Engine
{

    // TODO limit public methods and members

    public class Node
    {
        public int ID = 0;                  // this should be unique for any graph - not guaranteed to match ordinal of list 
        public string signifier = "";       // whatever the user wants to differentiate this node        
        // public List<Tag> tags;           // will allow for extended information on nodes

        public List<Edge> inboundEdges;
        public List<Edge> outboundEdges;

        public Node(string signifier, int id)
        {
            ID = id;
            inboundEdges = new List<Edge>();
            outboundEdges = new List<Edge>();
            this.signifier = signifier;
        }

        public bool Matches(Node b)
        {
            return (signifier == b.signifier);
        }

        public bool SignifierMatches(Node b)
        {
            return (signifier == b.signifier);
        }

        public void RemoveEdge( int targetNodeID )
        {
            inboundEdges.Remove(inboundEdges.Find(e => e.origin.ID == targetNodeID));
            outboundEdges.Remove(outboundEdges.Find(e => e.target.ID == targetNodeID));
        }

        public void RemoveEdges()
        {
            foreach ( Edge e in inboundEdges ) { e.origin.RemoveEdge(ID); }
            inboundEdges.Clear();
            outboundEdges.Clear();
        }
    }

    public class Edge
    {
        public Node origin;
        public Node target;

        public Edge(Node origin, Node target)
        {
            this.origin = origin;
            this.target = target;
            origin.outboundEdges.Add(this);
            target.inboundEdges.Add(this);
        }
    }

    [Serializable]
    public enum ApplicationTypes { All, First, First_x, x_from_First, Last, Last_x, x_from_Last, Every_x, Random, x_Random, Middle }

    [Serializable]
    public struct RuleApplication
    {
        public ApplicationTypes type;
        public int modifier;
    }

    public class Rule
    {
        [Serializable]
        public struct association
        {
            public int lhIndex;
            public int rhIndex;
            public int matchIndex;
        }

        public string name = "";

        Graph _LHS;
        Graph _RHS;

        public const int NO_ASSOCIATION = -1;

        public List<association> associations = new List<association>();

        public Graph LHS() { return _LHS; }
        public Graph RHS() { return _RHS; }

        public void AssignLHS(Graph graph) { _LHS = graph; }
        public void AssignRHS(Graph graph) { _RHS = graph; }

        public Rule() : this(new Graph(), new Graph()) {}

        public Rule(Graph LHS, Graph RHS)
        {        
            _LHS = LHS;
            _RHS = RHS;
            associations = new List<association>();
            ProcessAssociations();
        }

        public void ProcessAssociations()
        {
            _LHS.UpdateAdjacencyMatrix();
            _RHS.UpdateAdjacencyMatrix();

            foreach ( association a in associations )
            {
                if ( a.lhIndex == NO_ASSOCIATION )
                {
                    // addition
                }
                else if ( a.rhIndex == NO_ASSOCIATION )
                {
                    // removal
                }
                else
                {
                    // replacement
                }
            }            
        }

        internal void associate(Node n0, Node n1)
        {
            int index0 = n0 == null ? NO_ASSOCIATION : n0.ID;
            int index1 = n1 == null ? NO_ASSOCIATION : n1.ID;

            if ( index0 == NO_ASSOCIATION && index1 != NO_ASSOCIATION )
            {
                if (associations.Exists(a => (a.rhIndex == index1))){
                    var index = associations.FindIndex(a => (a.rhIndex == index1));
                    association current = associations[index];
                    current.lhIndex = NO_ASSOCIATION;
                    associations[index] = current;
                }
            }
            else if (index0 != NO_ASSOCIATION && index1 == NO_ASSOCIATION)
            {
                if (associations.Exists(a => (a.lhIndex == index0)))
                {
                    var index = associations.FindIndex(a => (a.lhIndex == index0));
                    association current = associations[index];
                    current.rhIndex = NO_ASSOCIATION;
                    associations[index] = current;
                }
            }

            if ( !associations.Exists( a => (a.lhIndex == index0 && a.rhIndex == index1) ) )
            {
                associations.Add(new association() { lhIndex = index0, rhIndex = index1, matchIndex = NO_ASSOCIATION });
            }
        }
    }

    public class Plan
    {
        public string name;
        public List<Rule> rules;
        public List<RuleApplication> applications;

        // A Plan will include a sequence of rules, and a corresponding sequence of indicators for how to apply the rules
        // for example, given a plan with many rules, the application information might tell us

        public Plan()
        {
            rules = new List<Rule>();
        }

        internal void AddRule(Rule r1, ApplicationTypes applicationType, int applicationModifier)
        {
            rules.Add(r1);
            RuleApplication application = new RuleApplication()
            {
                type = applicationType,
                modifier = applicationModifier
            };
            applications.Add(application);
        }

        internal Rule GetRule(int index)
        {
            if ( index < rules.Count && index < applications.Count && index >= 0)
            {
                return rules[index];
            }
            return null;
        }

        internal RuleApplication GetApplication(int index)
        {
            if (index < rules.Count && index < applications.Count && index >= 0)
            {
                return applications[index];
            }
            return new RuleApplication();
        }
    }

    public class Graph
    {
        public List<Node> nodes;
        //public List<Edge> edges;
        Dictionary<string, List<Node>> nodeSortList;
        Dictionary<string, List<Edge>> edgeSortList;
        public int[,] adjacencyMatrix;
        public bool adjacencyMatrixUpdateNeeded;
        public int nodeIDIndex = 0;

        public int nodeCount => nodes.Count;

        public Graph()
        {
            nodes = new List<Node>();
            //edges = new List<Edge>();
            nodeSortList = new Dictionary<string, List<Node>>();
            edgeSortList = new Dictionary<string, List<Edge>>();
        }

        public Node AddNode(Node n)
        {
            Node n1 = new Node(n.signifier, nodeIDIndex);
            nodeIDIndex++;
            return AddNodeInternal(n1);
        }

        public Node AddNode(string signifier)
        {
            Node n = new Node(signifier, nodeIDIndex);
            nodeIDIndex++;
            return AddNodeInternal(n);
        }

        public Node AddNodeInternal(Node n)
        {
            nodes.Add(n);
            adjacencyMatrixUpdateNeeded = true;
            return n;
        }

        // TODO will get connection type
        public void ConnectNodes(Node nodeA, Node nodeB)
        {
            if (!nodes.Contains(nodeA)) { throw new Exception("Cannot connect: nodeA not in graph"); }
            if (!nodes.Contains(nodeB)) { throw new Exception("Cannot connect: nodeB not in graph"); }

            // remove edges if they already exist
            nodeA.outboundEdges.RemoveAll(e => e.origin == nodeA && e.target == nodeB);
            nodeB.inboundEdges.RemoveAll(e => e.origin == nodeA && e.target == nodeB);
            Edge c = new Edge(nodeA, nodeB);

            nodeA.outboundEdges.RemoveAll(e => e.origin == nodeB && e.target == nodeA);
            nodeB.inboundEdges.RemoveAll(e => e.origin == nodeB && e.target == nodeA);
            Edge d = new Edge(nodeB, nodeA);
            
            adjacencyMatrixUpdateNeeded = true;
        }

        internal void ConnectDirected(Node nodeA, Node nodeB)
        {
            if (!nodes.Contains(nodeA)) { throw new Exception("Cannot connect: nodeA not in graph"); }
            if (!nodes.Contains(nodeB)) { throw new Exception("Cannot connect: nodeB not in graph"); }

            // remove edges if they already exist
            nodeA.outboundEdges.RemoveAll(e => e.origin == nodeA && e.target == nodeB);
            nodeB.inboundEdges.RemoveAll(e => e.origin == nodeA && e.target == nodeB);
            Edge c = new Edge(nodeA, nodeB);
            adjacencyMatrixUpdateNeeded = true;
        }

        public void RemoveNode( int nodeID )
        {
            Node target = nodes.Find(n => n.ID == nodeID);

            target.RemoveEdges();
            nodes.Remove(target);
            //edges.RemoveAll(e => e.origin.ID == nodeID);
            //edges.RemoveAll(e => e.target.ID == nodeID);

            adjacencyMatrixUpdateNeeded = true;
        }

        public void ReplaceNode( int nodeID, string Signifier )
        {
            Node target = nodes.Find(n => n.ID == nodeID);
            target.signifier = Signifier;

            // TODO what about edge changes?
            adjacencyMatrixUpdateNeeded = true;
        }

        public Node getNode(int nodeID)
        {
            return nodes.Find(n => n.ID == nodeID);
        }
        


        // TODO these seem redundant - why not pass the list of edges?

        internal HashSet<int> getOutgoingVertices(int subjectNode)
        {
            HashSet<int> Outgoing = new HashSet<int>();
            foreach ( Edge e in nodes[subjectNode].outboundEdges )
            {
                Outgoing.Add(e.target.ID);
            }
            return Outgoing;
        }

        internal HashSet<int> getIngoingVertices(int subjectNode)
        {
            HashSet<int> Incoming = new HashSet<int>();
            foreach (Edge e in nodes[subjectNode].inboundEdges)
            {
                Incoming.Add(e.origin.ID);
            }
            return Incoming;
        }

        internal int GetHighestNodeID()
        {
            int maxID = -1;
            foreach (Node n in nodes)
            {
                if (n.ID > maxID)
                {
                    maxID = n.ID;
                }
            }
            return maxID;
        }


        public void UpdateAdjacencyMatrix()
        {
            adjacencyMatrix = null;
            getAdjacencyMatrix();
        }


        public int[,] getAdjacencyMatrix()
        {
            if (adjacencyMatrix == null || adjacencyMatrixUpdateNeeded)
            {
                int k = nodes.Count;
                adjacencyMatrix = new int[k,k];
                for (int i = 0; i < k; i++) {
                    for (int j = 0; j < k; j++)
                    {
                        adjacencyMatrix[i,j] = 0; // initialize entries to 0
                    }
                }
                foreach (Node n in nodes)
                {
                    foreach (Edge e in n.inboundEdges)
                    {
                        adjacencyMatrix[e.target.ID, e.origin.ID] = 1; // change entries to 1 if there is an edge
                    }
                    foreach (Edge e in n.outboundEdges)
                    {
                        adjacencyMatrix[e.origin.ID, e.target.ID] = 1; // change entries to 1 if there is an edge
                    }
                }
                adjacencyMatrixUpdateNeeded = false;
            }
            return adjacencyMatrix;
        }

    }
}