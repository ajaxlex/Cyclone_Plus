using System.Collections;
using System.Collections.Generic;
//using UnityEngine;

namespace VertexHerder.Cyclone.Engine
{
    // An Improved Algorithm For Searching Large Graphs ( Vento, Foggia et al )
    // https://pdfs.semanticscholar.org/f3e1/0bd7521ec6263a58fdaa4369dfe8ad50888c.pdf

    public class SearchData
    {
        private const int NO_MAPPING = -1;

        public Graph graph;

        // ordered list which stores the index for the target graph's node which maps to the alternate graph's node at index n 
        // ( or NO_MAPPING is there is no association )

        // This is the set of matched nodes
        public List<int> core;

        // the in terminal set for the target graph -
        // the set of nodes that are not yet matched for the target graph, but are predecessors of a matched node.

        // This is the set of unmatched nodes from inbound edges
        public HashSet<int> terminalIn;

        // the out terminal set for the target graph -
        // the set of nodes that are not yet matched for the target graph, but are successors of a matched node. 

        // This is the set of unmatched nodes from outbound edges
        public HashSet<int> terminalOut;

        // depth of the state in which the node entered the terminal in set
        public List<int> inDepth;

        // depth of the state in which the node entered the terminal out set
        public List<int> outDepth;

        public HashSet<int> unmapped;

        public int graphSize;

        public SearchData( Graph target, int maxSize )
        {
            graph = target;

            graphSize = graph.nodeCount;

            core = new List<int>(graphSize);

            // maxsize because the associations could include the totality of the larger graph's size
            terminalIn = new HashSet<int>();

            terminalOut = new HashSet<int>();

            unmapped = new HashSet<int>(); // 2*graphSize

            inDepth = new List<int>(graphSize);
            outDepth = new List<int>(graphSize);

            for (int i = 0; i < graphSize; i++)
            {
                core.Add(-1);
                inDepth.Add(-1);
                outDepth.Add(-1);
                unmapped.Add(i);
            }
        }

        public bool inMatch(int n)
        {
            return (core[n] != NO_MAPPING);
        }

        public bool inTerminalIn(int n)
        {
            return ((core[n] == NO_MAPPING) && (inDepth[n] != NO_MAPPING));
        }

        public bool inTerminalOut(int n)
        {
            return ((core[n] == NO_MAPPING) && (outDepth[n] != NO_MAPPING));
        }

        public bool notInN(int n)
        {
            return ((core[n] == NO_MAPPING) && (inDepth[n] == NO_MAPPING) && (outDepth[n] == NO_MAPPING));
        }

        /*
        public bool inDepthIn(int n)
        {
            return (inTerminalIn(n) || inTerminalOut(n));
        }
        */

        public void updateFirst( int n, int m )
        {
            // include pair (n,m) into the mapping

            core[n] = m;
            unmapped.Remove(n);
            terminalIn.Remove(n);
            terminalOut.Remove(n);
        }

        public void updateDepths(int n, int depth)
        {
            // update in/out arrays
            // updates needed for nodes entering Tin/Tout sets on this level
            // no updates needed for nodes which entered these sets before

            Node nTmp = graph.nodes[n];

            foreach (Edge e in nTmp.inboundEdges)
            {
                if (inDepth[e.origin.ID] == NO_MAPPING)
                {
                    inDepth[e.origin.ID] = depth;
                    if (!inMatch(e.origin.ID))
                    {
                        terminalIn.Add(e.origin.ID);
                    }
                }
            }
            foreach (Edge e in nTmp.outboundEdges)
            {
                if (outDepth[e.target.ID] == NO_MAPPING)
                {
                    outDepth[e.target.ID] = depth;
                    if (!inMatch(e.target.ID))
                    {
                        terminalOut.Add(e.target.ID);
                    }
                }
            }
        }

        public void backtrack(int n, int depth)
        {
            core[n] = NO_MAPPING;
            unmapped.Add(n);

            for ( int i=0; i<core.Count; i++)
            {
                if( inDepth[i] == depth)
                {
                    inDepth[i] = NO_MAPPING;
                    terminalIn.Remove(i);
                }
                if (outDepth[i] == depth)
                {
                    outDepth[i] = NO_MAPPING;
                    terminalOut.Remove(i);
                }
            }

            // put n / m back into Tin and Tout sets if necessary
            if (inTerminalIn(n))
                terminalIn.Add(n);
            if (inTerminalOut(n))
                terminalOut.Add(n);
        }
    }


    public class VF2State
    {
        private const int NO_MAPPING = -1;

        public SearchData subject;
        public SearchData pattern;

        public int searchDepth; // current depth of search

        //public int matchedNodeCount { get => foundPatternCount; set => foundPatternCount = value; }

        public VF2State(Graph subjectG, Graph patternG)
        {
            subject = new SearchData(subjectG, subjectG.nodeCount);
            pattern = new SearchData(patternG, subjectG.nodeCount);
            searchDepth = 0;
        }
        
        internal void extend(KeyValuePair<int, int> pair)
        {
            int n = pair.Key;
            int m = pair.Value;

            // extends this state with new searched stuff
            subject.updateFirst(n,m);
            pattern.updateFirst(m,n);

            searchDepth++; // increase depth (we moved down one level in the search tree)

            subject.updateDepths(n, searchDepth);
            pattern.updateDepths(m, searchDepth);
        }

        internal void backtrack(KeyValuePair<int, int> pair)
        {
            int n = pair.Key;
            int m = pair.Value;

            subject.backtrack(n, searchDepth);
            pattern.backtrack(m, searchDepth);
            searchDepth--;
        }
    }
}
