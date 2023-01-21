using System;
using System.Collections;
using System.Collections.Generic;

namespace VertexHerder.Cyclone.Engine
{
    // An Improved Algorithm For Searching Large Graphs ( Vento, Foggia et al )
    // https://pdfs.semanticscholar.org/f3e1/0bd7521ec6263a58fdaa4369dfe8ad50888c.pdf

    public class VF2Match
    {
        private const int NO_MAPPING = -1;

        VF2State state;
        Graph subject;
        Graph pattern;
        List<List<int>> patternMatches;

        public List<List<int>> FindPatternMatches(Graph subject, Graph pattern)
        {
            this.subject = subject;
            this.pattern = pattern;

            state = new VF2State(subject, pattern);
            Match(state, subject, pattern);
            return patternMatches;
        }

        private bool Match(VF2State state, Graph subject, Graph pattern)
        {
            // test for coverage of sub
            if ( SearchIsComplete(state) )
            {
                // matches can be found in
                // state.subject.core where value is not NO_MAPPING
                AddMatches(state.subject.core, pattern.nodeCount);
                return true;               
            }
            else
            {
                SortedDictionary<int, int> candidates = GetCandidatePairs(state);
                foreach ( KeyValuePair<int,int> pair in candidates )
                {
                    if (ConstraintsAreSatisfied(pair)) { 
                        state.extend( pair );
                        Match(state, subject, pattern);
                        state.backtrack( pair );
                    }
                }
            }
            return false;
        }

        private void AddMatches(List<int> core, int patternLength)
        {
            if ( patternMatches == null )
            {
                patternMatches = new List<List<int>>();
            }

            if (core.Count > 0) {
                List<int> matches = new List<int>();
                for (int i = 0; i < patternLength; i++)
                {
                    matches.Add(NO_MAPPING);
                }

                for ( int i=0; i < core.Count; i++ )
                {
                    if ( core[i] != NO_MAPPING )
                    {
                        matches[core[i]] = i;
                    }
                }

                patternMatches.Add(matches);
            }

        }

        private bool SearchIsComplete(VF2State state)
        {
            return (state.searchDepth == pattern.nodeCount);
        }

        private SortedDictionary<int,int> GetCandidatePairs( VF2State state )
        {
            if (state.searchDepth == 0)
            {
                return GetNodePairList(state.subject.unmapped, state.pattern.unmapped);
            }

            SortedDictionary<int,int> inmap = GetNodePairList(state.subject.terminalIn, state.pattern.terminalIn);

            if (inmap.Count > 0) { return inmap; }

            SortedDictionary<int, int> outmap = GetNodePairList(state.subject.terminalOut, state.pattern.terminalOut);

            return outmap;
        }

        private SortedDictionary<int, int> GetNodePairList(HashSet<int> subjectRemaining, HashSet<int> patternRemaining)
        {
            var pairList = new SortedDictionary<int, int>();

            //int patternID = pattern.GetHighestNodeID();

            int patternID = -1;
            
            foreach ( int i in patternRemaining )
            {
                if ( i > patternID)
                {
                    patternID = i;
            }

                }
            if (patternID > -1)
            {
                foreach (Node n in subject.nodes)
                {
                    pairList.Add(n.ID, patternID);
                }
            }

            return pairList;
        }





        #region constraints

        private bool ConstraintsAreSatisfied( KeyValuePair<int, int> pair )
        {
            var subjectID = pair.Key;
            var patternID = pair.Value;

            return (ConfirmSemanticMatch(state, subjectID, patternID) && ConfirmFeasabilityMatch(state, subjectID, patternID));
        }

        private bool ConfirmSemanticMatch(VF2State state, int subjectID, int patternID)
        {
            return (state.subject.graph.nodes[subjectID].signifier == state.pattern.graph.nodes[patternID].signifier);
        }

        private bool ConfirmFeasabilityMatch(VF2State state, int subjectID, int patternID)
        {
            bool passed = true;
            passed = passed && checkRpredAndRsucc(state, subjectID, patternID); // check Rpred / Rsucc conditions (subgraph isomorphism definition)
            passed = passed && CheckRin(state, subjectID, patternID);
            passed = passed && CheckRout(state, subjectID, patternID);
            passed = passed && CheckRnew(state, subjectID, patternID);
            return passed; // return result
        }

        private bool checkRpredAndRsucc(VF2State state, int subjectID, int patternID)
        {
            Boolean passed = true;

            int[,] amPattern = state.pattern.graph.getAdjacencyMatrix();
            int[,] amSubject = state.subject.graph.getAdjacencyMatrix();

            // check if the structure of the (partial) model graph is also present in the (partial) pattern graph
            // if a predecessor of n has been mapped to a node n' before, then n' must be mapped to a predecessor of m
            Node nTmp = state.subject.graph.nodes[subjectID];
            foreach (Edge e in nTmp.inboundEdges)
            {
                if (state.subject.core[e.origin.ID] != NO_MAPPING)
                {
                    passed = passed && (amPattern[state.subject.core[e.origin.ID],patternID] == 1);
                }
            }
            // if a successor of n has been mapped to a node n' before, then n' must be mapped to a successor of m
            foreach (Edge e in nTmp.outboundEdges)
            {
                if (state.subject.core[e.target.ID] != NO_MAPPING)
                {
                    passed = passed && (amPattern[patternID,state.subject.core[e.target.ID]] == 1);
                }
            }

            // check if the structure of the (partial) pattern graph is also present in the (partial) model graph
            // if a predecessor of m has been mapped to a node m' before, then m' must be mapped to a predecessor of n
            Node mTmp = state.pattern.graph.nodes[patternID];
            foreach (Edge e in mTmp.inboundEdges)
            {
                if (state.pattern.core[e.origin.ID] != NO_MAPPING)
                {
                    passed = passed && (amSubject[state.pattern.core[e.origin.ID],subjectID] == 1);
                }
            }
            // if a successor of m has been mapped to a node m' before, then m' must be mapped to a successor of n
            foreach (Edge e in mTmp.outboundEdges)
            {
                if (state.pattern.core[e.target.ID] != NO_MAPPING)
                {
                    passed = passed && (amSubject[subjectID, state.pattern.core[e.target.ID]] == 1);
                }
            }

            return passed; // return the result

        }

        // In Rule
        // The number predecessors/successors of the target node that are in T1in 
        // must be larger than or equal to those of the query node that are in T2in
        private bool CheckRin(VF2State state, int subjectID, int patternID)
        {
            HashSet<int> T1in = new HashSet<int>(state.subject.terminalIn);
            var ingoing1 = state.subject.graph.getIngoingVertices(subjectID);
            T1in.RemoveWhere(t => ingoing1.Contains(t));

            HashSet<int> T2in = new HashSet<int>(state.pattern.terminalIn);
            var ingoing2 = state.pattern.graph.getIngoingVertices(patternID);
            T2in.RemoveWhere(t => ingoing2.Contains(t));

            bool firstExp = T1in.Count >= T2in.Count;

            HashSet<int> T1in2 = new HashSet<int>(state.subject.terminalIn);
            var outgoing1 = state.subject.graph.getOutgoingVertices(subjectID);
            T1in2.RemoveWhere(t => outgoing1.Contains(t));

            HashSet<int> T2in2 = new HashSet<int>(state.pattern.terminalIn);
            var outgoing2 = state.pattern.graph.getOutgoingVertices(patternID);
            T2in2.RemoveWhere(t => outgoing2.Contains(t));

            bool secondExp = T1in2.Count >= T2in2.Count;

            return firstExp && secondExp;
        }

        // Out Rule
        // The number predecessors/successors of the target node that are in T1out 
        // must be larger than or equal to those of the query node that are in T2out
        private bool CheckRout(VF2State state, int subjectID, int patternID)
        {
            HashSet<int> T1out = new HashSet<int>(state.subject.terminalIn);
            var ingoing1 = state.subject.graph.getIngoingVertices(subjectID);
            T1out.RemoveWhere(t => !ingoing1.Contains(t));

            HashSet<int> T2out = new HashSet<int>(state.pattern.terminalIn);
            var ingoing2 = state.pattern.graph.getIngoingVertices(patternID);
            T2out.RemoveWhere(t => !ingoing2.Contains(t));

            bool firstExp = T1out.Count >= T2out.Count;

            HashSet<int> T1out2 = new HashSet<int>(state.subject.terminalIn);
            var outgoing1 = state.subject.graph.getOutgoingVertices(subjectID);
            T1out2.RemoveWhere(t => !outgoing1.Contains(t));

            HashSet<int> T2out2 = new HashSet<int>(state.pattern.terminalIn);
            var outgoing2 = state.pattern.graph.getOutgoingVertices(patternID);
            T2out2.RemoveWhere(t => !outgoing2.Contains(t));

            bool secondExp = T1out2.Count >= T2out2.Count;

            return firstExp && secondExp;
        }


        private bool CheckRnew(VF2State state, int subjectID, int patternID)
        {
            Node subjectNode = state.subject.graph.nodes[subjectID];
            Node patternNode = state.pattern.graph.nodes[patternID];

            int subjectPredecessorCount = 0; int subjectSuccessorCount = 0;
            int patternPredecessorCount = 0; int patternSuccessorCount = 0;

            foreach ( Edge e in subjectNode.inboundEdges)
            {
                if ( state.subject.notInN(e.origin.ID)) { subjectPredecessorCount++; }
            }
            foreach (Edge e in subjectNode.outboundEdges)
            {
                if (state.subject.notInN(e.target.ID)) { subjectSuccessorCount++; }
            }

            foreach (Edge e in patternNode.inboundEdges)
            {
                if (state.pattern.notInN(e.origin.ID)) { patternPredecessorCount++; }
            }
            foreach (Edge e in patternNode.outboundEdges)
            {
                if (state.pattern.notInN(e.target.ID)) { patternSuccessorCount++; }
            }

            if (subjectPredecessorCount < patternPredecessorCount || subjectSuccessorCount < patternSuccessorCount)
            {
                return false;
            }
            return true;

        }


        #endregion







    }
}

