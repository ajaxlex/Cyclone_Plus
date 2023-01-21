using System;
using System.Collections;
using System.Collections.Generic;


namespace VertexHerder.Cyclone.Engine
{
    public class GrammarEngine
    {
        public Graph workingGraph;
        private System.Random rnd;

        public GrammarEngine()
        {
            workingGraph = new Graph();
            SetRandomSeed(1);
        }

        public void SetRandomSeed( int seed )
        {
            rnd = new Random(seed);
        }

        internal void LoadWorkingGraph(Graph g)
        {
            workingGraph = g;
        }

        internal void RunPlan(Plan plan)
        {
            if ( workingGraph != null )
            {
                for (int i = 0; i < plan.rules.Count; i++)
                {
                    Rule r = plan.GetRule(i);
                    RuleApplication ra = plan.GetApplication(i);

                    VF2Match vfm = new VF2Match();
                    List<List<int>> matches = vfm.FindPatternMatches(workingGraph, r.LHS());
                    int index = 0;
                    int modifier = (ra.modifier <= matches.Count) ? ra.modifier : matches.Count;

                    switch ( ra.type )
                    {
                        case ApplicationTypes.Random:
                            index = rnd.Next(0, matches.Count - 1);
                            applyRule(r, matches[index]);
                            break;
                        case ApplicationTypes.x_Random:
                            var chosen = getXRandomMatches(modifier, matches);
                            for ( int t = 0; t < chosen.Count; t++) { applyRule(r, chosen[t]); }
                            break;
                        case ApplicationTypes.All:
                            for ( int t = 0; t < matches.Count; t++) { applyRule(r, matches[t]); }
                            break;
                        case ApplicationTypes.First:
                            applyRule(r, matches[0]);
                            break;
                        case ApplicationTypes.Last:
                            applyRule(r, matches[matches.Count - 1]);
                            break;
                        case ApplicationTypes.First_x:
                            for ( int t = 0; t < modifier; t++) { applyRule(r, matches[t]); }
                            break;
                        case ApplicationTypes.Last_x:
                            for ( int t = matches.Count - modifier; t < matches.Count; t++) { applyRule(r, matches[t]); }
                            break;
                        case ApplicationTypes.Every_x:
                            for ( int t = 0; t < matches.Count; t+=modifier) { applyRule(r, matches[t]); }
                            break;
                        case ApplicationTypes.Middle:
                            applyRule(r, matches[(int)(matches.Count / 2)]);
                            break;
                        case ApplicationTypes.x_from_First:
                            applyRule(r, matches[modifier]);
                            break;
                        case ApplicationTypes.x_from_Last:
                            applyRule(r, matches[matches.Count - modifier]);
                            break;
                        default:
                            index = rnd.Next(0, matches.Count - 1);
                            applyRule(r, matches[index]);
                            break;
                    }


                    // here is where rule applications will be dealt with

                    // ApplicationTypes.Random - apply to single random
                    // ApplicationTypes.x_Random - apply to multiple random
                    // ApplicationTypes.All - apply to all
                    // ApplicationTypes.Every_x - apply every x matches
                    // ApplicationTypes.First - apply to first match ( ? closest to start ? )
                    // ApplicationTypes.Last - apply to last match ( ? farthest from start ? )
                    // ApplicationTypes.First_x, Last_x - apply to the first or last x matches
                    // ApplicationTypes.x_from_First, x_from_Last - apply to the xth from the first or last match
                    // ApplicationTypes.Middle - apply to the middlemost match ( ? between start and end ? ? middle of list ? ) 

                    // types which apply to multiple matches shall first determine how many apply.  This may require a search to start with.

                    // TODO one important decision - must a search restart on any rule that changes the shape of the working graph, 
                    // or can the saved matches be reused if they are still valid?  currently attempting the latter

                    if (matches.Count > 0)
                    {
                        List<int> chosenMatch = chooseMatch(matches);
                        // apply rule
                        applyRule(r, chosenMatch);
                    }
                }

            }
        }

        private List<List<int>> getXRandomMatches( int x, List<List<int>> matches )
        {
            List<List<int>> chosen = new List<List<int>>();

            int escape = 100 * matches.Count;
            int index = 0;
            List<int> pickList = new List<int>();
            do
            {
                index = rnd.Next(0, matches.Count - 1);
                if ( !pickList.Contains(index) )
                {
                    pickList.Add(index);
                    chosen.Add(matches[index]);
                } else
                {
                    escape--;
                }
            } while (pickList.Count < x && escape > 0);

            return chosen;
        }

        private void applyRule(Rule r, List<int> chosenMatch)
        {
            // remove edges between nodes that are both included in chosenMatch
            removeEdges(chosenMatch);

            // associate matches in working graph with corresponding association structs
            for ( int c=0; c<chosenMatch.Count; c++)
            {
                int index = r.associations.FindIndex(a => a.lhIndex == c);
                Rule.association match = r.associations[index];
                match.matchIndex = chosenMatch[c];
                r.associations[index] = match;
            }

            // transform working graph
            for ( int i=0; i<r.associations.Count; i++)
            {
                Rule.association a = r.associations[i];

                if ( a.lhIndex == Rule.NO_ASSOCIATION && a.rhIndex == Rule.NO_ASSOCIATION ) { continue; }
                // add node if it's on the right side, with no left side association
                else if ( a.lhIndex == Rule.NO_ASSOCIATION )
                {
                    var corresponding = r.RHS().nodes[a.rhIndex];
                    var addedNode = workingGraph.AddNode(corresponding);
                    a.matchIndex = addedNode.ID;
                    r.associations[i] = a;
                }
                // remove node if it's on the left side, with no right side association
                else if ( a.rhIndex == Rule.NO_ASSOCIATION)
                {
                    workingGraph.RemoveNode(a.matchIndex);
                }
                // replace node if its on both sides
                else
                {
                    workingGraph.ReplaceNode(a.matchIndex, r.RHS().getNode(a.rhIndex).signifier);
                }
            }

            // replace all edges as specified in RHS
            replaceEdges(r, chosenMatch);
        }

        private void removeEdges(List<int> chosenMatch)
        {
            foreach ( int index in chosenMatch )
            {
                Node target = workingGraph.getNode(index);

                List<Edge> outboundEdges = target.outboundEdges;
                for ( int i=outboundEdges.Count-1; i>=0; i--)
                {
                    var current = outboundEdges[i];
                    if ( chosenMatch.Contains(current.target.ID))
                    {
                        outboundEdges.RemoveAt(i);
                    }
                }
                List<Edge> inboundEdges = target.inboundEdges;
                for (int i = inboundEdges.Count - 1; i >= 0; i--)
                {
                    var current = inboundEdges[i];
                    if (chosenMatch.Contains(current.origin.ID))
                    {
                        inboundEdges.RemoveAt(i);
                    }
                }

            }
        }

        private void replaceEdges(Rule r, List<int> chosenMatch)
        {
            for ( int i = 0; i < r.RHS().nodes.Count; i++)
            {
                Node templateNode = r.RHS().nodes[i];
                foreach (Edge e in templateNode.outboundEdges)
                {
                    int templateOrigin = e.origin.ID;
                    int templateTarget = e.target.ID;
                    int correspondingOrigin = r.associations.Find( a => a.rhIndex == templateOrigin ).matchIndex;
                    int correspondingTarget = r.associations.Find( a => a.rhIndex == templateTarget ).matchIndex;

                    workingGraph.ConnectDirected(workingGraph.getNode(correspondingOrigin), workingGraph.getNode(correspondingTarget));
                }
                foreach (Edge e in templateNode.inboundEdges)
                {
                    int templateOrigin = e.origin.ID;
                    int templateTarget = e.target.ID;
                    int correspondingOrigin = r.associations.Find(a => a.rhIndex == templateOrigin).matchIndex;
                    int correspondingTarget = r.associations.Find(a => a.rhIndex == templateTarget).matchIndex;

                    workingGraph.ConnectDirected(workingGraph.getNode(correspondingTarget), workingGraph.getNode(correspondingOrigin));
                }
            }
        }

        private List<int> chooseMatch(List<List<int>> matches)
        {
            return matches[0];
        }
    }
}




