using System;
using System.Collections;
using System.Collections.Generic;
using VertexHerder.Cyclone.Engine;

public class EngineTest
{
    public EngineTest()
    {
    }

    #region graph consistency



    #endregion


    #region matchtests

    internal void CanAttemptVF2Match()
    {
        Graph g = CreatePatternGraph();

        Graph g2 = new Graph();
        Node n3 = g2.AddNode("C");
        Node n4 = g2.AddNode("A");
        Node n5 = g2.AddNode("B");
        g2.ConnectNodes(n3, n4);
        g2.ConnectNodes(n4, n5);

        VF2Match vfm = new VF2Match();
        List<List<int>> matches = vfm.FindPatternMatches(g2, g);

        Assert("basic match found ", matches.Count == 1);
    }

    internal void CanMultipleVF2Match()
    {
        Graph g = CreatePatternGraph();
        Graph g2 = CreateBasicGraph();

        VF2Match vfm = new VF2Match();
        List<List<int>> matches = vfm.FindPatternMatches(g2, g);

        Assert("multiple match found ", matches.Count == 2);
    }

    internal void CanDirectedVF2Match()
    {
        Graph g = new Graph();
        Node n1 = g.AddNode("A");
        Node n2 = g.AddNode("B");
        g.ConnectDirected(n1, n2);

        Graph g2 = CreateBasicGraph();

        VF2Match vfm = new VF2Match();
        List<List<int>> matches = vfm.FindPatternMatches(g2, g);

        Assert("directed match found ", matches.Count == 1);
    }

    #endregion


    #region graph transformations

    internal void CanApplySimpleRule()
    {
        Graph subject = CreateBasicGraph();
        Rule r = CreateBasicRule();
        Plan p = new Plan();
        GrammarEngine ge = new GrammarEngine();

        bool findReplacementSignifierBefore = ( ge.workingGraph.nodes.Find(n => n.signifier == "X") == null );
        ge.LoadWorkingGraph(subject);
        p.AddRule(r, ApplicationTypes.First, 0);
        ge.RunPlan(p);
        bool findReplacementSignifierAfter = ( ge.workingGraph.nodes.Find(n => n.signifier == "X") == null );

        Assert("Didn't find signifier before, did find after", (findReplacementSignifierBefore == true && findReplacementSignifierAfter == false));

    }

    internal void CanRemoveNode()
    {
        Graph subject = CreateBasicGraph();
        Rule r = CreateRuleForRemoval();
        Plan p = new Plan();
        GrammarEngine ge = new GrammarEngine();

        // TODO the test setup here is brittle - the magic number should be discoverable
        Node target = subject.nodes.Find(n => n.ID == 2);
        int previous = subject.nodes.Count;

        ge.LoadWorkingGraph(subject);
        p.AddRule(r, ApplicationTypes.First, 0);
        ge.RunPlan(p);

        Assert("nodes count after removal", ge.workingGraph.nodes.Count == previous - 1);
        Assert("expected node removed", ge.workingGraph.nodes.Find(n => n.ID == target.ID) == null );
    }

    internal void CanAddNodes()
    {
        Graph subject = CreateBasicGraph();
        Rule r = CreateRuleForAddition();
        Plan p = new Plan();
        GrammarEngine ge = new GrammarEngine();

        int previous = subject.nodes.Count;

        ge.LoadWorkingGraph(subject);
        p.AddRule(r, ApplicationTypes.First, 0);
        ge.RunPlan(p);

        Assert("nodes count after addition ", ge.workingGraph.nodes.Count == previous + 1);
        Assert("nodes found added ", ge.workingGraph.nodes.Find(n => n.signifier == "F") != null);
    }

    internal void CanDoMultipleTransformations()
    {
        Graph subject = CreateBasicGraph();
        Rule r = CreateRuleForRemoval();
        Plan p = new Plan();
        GrammarEngine ge = new GrammarEngine();

        ge.LoadWorkingGraph(subject);
        p.AddRule(r, ApplicationTypes.First, 0);
        ge.RunPlan(p);
    }

    internal void CanInsertNodes()
    {
        Graph subject = CreateBasicGraph();

        WriteAssociations("Insert Nodes Test Map PRE ", subject);

        Rule r = CreateRuleForInsertion();
        Plan p = new Plan();
        GrammarEngine ge = new GrammarEngine();

        int previous = subject.nodes.Count;

        ge.LoadWorkingGraph(subject);

        p.AddRule(r, ApplicationTypes.First, 0);
        ge.RunPlan(p);

        Assert("nodes count after insertion ", ge.workingGraph.nodes.Count == previous + 1);

        WriteAssociations("Insert Nodes Test Map POST ", ge.workingGraph);
    }

    #endregion


    #region plan execution

    internal void CanRunTwoPartPlan()
    {
        Graph subject = CreateOriginGraph();

        Rule ri = CreateRuleForInsertion();
        Rule rr = CreateRuleForRemoval();
        Plan p = new Plan();
        GrammarEngine ge = new GrammarEngine();

        int previous = subject.nodes.Count;

        ge.LoadWorkingGraph(subject);

        p.AddRule(ri, ApplicationTypes.First, 0);
        p.AddRule(rr, ApplicationTypes.First, 0);

        ge.RunPlan(p);

        // Assert("nodes count after addition ", ge.workingGraph.nodes.Count == previous + 1);
        // Assert("nodes found added ", ge.workingGraph.nodes.Find(n => n.signifier == "F") != null);

    }

    #endregion

    // TODO test bigger patterns
    // TODO test wildcard rule elements

    #region arrange helpers

    internal Graph CreatePatternGraph()
    {
        Graph pattern = new Graph();
        Node n1 = pattern.AddNode("A");
        Node n2 = pattern.AddNode("B");
        pattern.ConnectNodes(n1, n2);
        return pattern;
    }

    internal Graph CreateBasicGraph()
    {
        /*
          
         C0 - A1 - B2 - A5 
                \
                 A3 -> B4

         */

        Graph g2 = new Graph();

        Node n0 = g2.AddNode("C");
        Node n1 = g2.AddNode("A");
        Node n2 = g2.AddNode("B");
        Node n3 = g2.AddNode("A");
        Node n4 = g2.AddNode("B");
        Node n5 = g2.AddNode("A");

        g2.ConnectNodes(n0, n1);
        g2.ConnectNodes(n1, n2);
        g2.ConnectNodes(n1, n3);
        g2.ConnectDirected(n3, n4);
        g2.ConnectNodes(n2, n5);

        return g2;
    }

    internal Graph CreateComplexGraph()
    {

        /*           D3 - A4 - B9
                    /      ^   |
        C0 - A1 - B2       |   A8
                    \      |   |
                     A7 - B5 - C6
        */
        Graph g2 = new Graph();

        Node n0 = g2.AddNode("C");
        Node n1 = g2.AddNode("A");
        Node n2 = g2.AddNode("B");
        Node n3 = g2.AddNode("D");
        Node n4 = g2.AddNode("A");
        Node n5 = g2.AddNode("B");
        Node n6 = g2.AddNode("C");
        Node n7 = g2.AddNode("A");
        Node n8 = g2.AddNode("A");
        Node n9 = g2.AddNode("B");

        g2.ConnectNodes(n0, n1);
        g2.ConnectNodes(n1, n2);
        g2.ConnectNodes(n2, n3);
        g2.ConnectNodes(n3, n4);
        g2.ConnectNodes(n4, n9);
        g2.ConnectDirected(n5, n4);
        g2.ConnectNodes(n2, n7);
        g2.ConnectNodes(n7, n5);
        g2.ConnectNodes(n5, n6);
        g2.ConnectNodes(n6, n8);
        g2.ConnectNodes(n8, n9);

        return g2;
    }

    internal Graph CreateOriginGraph()
    {
        Graph g2 = new Graph();

        Node n0 = g2.AddNode("S");

        return g2;
    }

    internal Rule CreateBasicRule()
    {
        Graph gLHS = CreatePatternGraph();
        Graph gRHS = CreatePatternGraph();
        gRHS.nodes[0].signifier = "X";

        Rule r = new Rule(gLHS, gRHS);
        r.associate(gLHS.nodes[0], gRHS.nodes[0]);
        r.associate(gLHS.nodes[1], gRHS.nodes[1]);
        r.ProcessAssociations();
        return r;
    }

    internal Rule CreateRuleForRemoval()
    {
        Rule r = CreateBasicRule();
        r.RHS().RemoveNode(1);
        r.associate(r.LHS().nodes[1], null);
        r.ProcessAssociations();
        return r;
    }

    internal Rule CreateRuleForAddition()
    {
        Rule r = CreateBasicRule();
        Node n = r.RHS().AddNode("F");
        Node origin = r.RHS().nodes[1];
        r.RHS().ConnectNodes(origin, n);
        r.associate(null, n);
        r.ProcessAssociations();
        return r;
    }

    internal Rule CreateRuleForInsertion()
    {
        Graph gLHS = new Graph();
        Node n0 = gLHS.AddNode("A");
        Node n1 = gLHS.AddNode("B");
        gLHS.ConnectNodes(n0, n1);

        Graph gRHS = new Graph();
        Node n2 = gRHS.AddNode("A");
        Node n3 = gRHS.AddNode("C");
        Node n4 = gRHS.AddNode("B");
        gRHS.ConnectNodes(n2, n3);
        gRHS.ConnectNodes(n3, n4);
 
        Rule r = new Rule(gLHS, gRHS);
        r.associate(n0,n2);
        r.associate(n1,n4);
        r.associate(null, n3);
        r.ProcessAssociations();
        return r;
    }
    #endregion


    #region serialization

    internal string BasicGraphJson()
    {
        var jsonic = @"{""name"":"""",""version"":0.0,""nodes"":[{""ID"":0,""signifier"":""C"",""incomingEdges"":[{""oid"":1,""tid"":0,""type"":0}],""outgoingEdges"":[{""oid"":0,""tid"":1,""type"":0}],""guiDefaultPosition"":{""x"":100.0,""y"":100.0}},{""ID"":1,""signifier"":""A"",""incomingEdges"":[{""oid"":0,""tid"":1,""type"":0},{""oid"":2,""tid"":1,""type"":0},{""oid"":3,""tid"":1,""type"":0}],""outgoingEdges"":[{""oid"":1,""tid"":0,""type"":0},{""oid"":1,""tid"":2,""type"":0},{""oid"":1,""tid"":3,""type"":0}],""guiDefaultPosition"":{""x"":100.0,""y"":100.0}},{""ID"":2,""signifier"":""B"",""incomingEdges"":[{""oid"":1,""tid"":2,""type"":0},{""oid"":5,""tid"":2,""type"":0}],""outgoingEdges"":[{""oid"":2,""tid"":1,""type"":0},{""oid"":2,""tid"":5,""type"":0}],""guiDefaultPosition"":{""x"":100.0,""y"":100.0}},{""ID"":3,""signifier"":""A"",""incomingEdges"":[{""oid"":1,""tid"":3,""type"":0}],""outgoingEdges"":[{""oid"":3,""tid"":1,""type"":0},{""oid"":3,""tid"":4,""type"":0}],""guiDefaultPosition"":{""x"":100.0,""y"":100.0}},{""ID"":4,""signifier"":""B"",""incomingEdges"":[{""oid"":3,""tid"":4,""type"":0}],""outgoingEdges"":[],""guiDefaultPosition"":{""x"":100.0,""y"":100.0}},{""ID"":5,""signifier"":""A"",""incomingEdges"":[{""oid"":2,""tid"":5,""type"":0}],""outgoingEdges"":[{""oid"":5,""tid"":2,""type"":0}],""guiDefaultPosition"":{""x"":100.0,""y"":100.0}}]}";
        return jsonic;
    }

    internal void SaveGraphList()
    {
        Graph subject = CreateBasicGraph();

        PersistentGraphList pgl = new PersistentGraphList();
        pgl.graphs = new List<PersistentGraphData>();
        pgl.graphs.Add(SerializationUtility.TranslateToSerialGraph(subject));
        SerializationUtility.SaveGraphs(pgl);
    }

    internal void LoadGraphList()
    {
        var graphList = SerializationUtility.LoadGraphs();
        foreach ( Graph g in graphList )
        {
            var x = 100;
        }
    }

    #endregion

    #region test helpers

    private void Assert(string test, bool v)
    {
        ConsoleColor baseColor = ConsoleColor.White;

        string result = "FAILED";
        ConsoleColor testColor = ConsoleColor.Red;

        if ( v)
        {
            result = "PASSED";
            testColor = ConsoleColor.Green;
        }

        Console.ForegroundColor = baseColor;
        Console.Write(test + " ");
        Console.ForegroundColor = testColor;
        Console.WriteLine(result);
        Console.ForegroundColor = baseColor;

    }

    private void WriteAssociations(string title, Graph g)
    {
        Console.WriteLine(title);

        foreach ( Node n in g.nodes ){
            Console.Write(n.signifier + n.ID + ": ");
            if ( n.outboundEdges.Count > 0)
            {
                Console.Write("out {");
                for (int i=0; i < n.outboundEdges.Count; i++)
                {
                    var current = n.outboundEdges[i].target;
                    Console.Write(current.signifier + current.ID);
                    if ( i < n.outboundEdges.Count - 1) { Console.Write(", "); }
                }
                Console.Write("} ");

            }
            if (n.inboundEdges.Count > 0)
            {
                Console.Write("in {");
                for (int i = 0; i < n.inboundEdges.Count; i++)
                {
                    var current = n.inboundEdges[i].origin;
                    Console.Write(current.signifier + current.ID);
                    if (i < n.inboundEdges.Count - 1) { Console.Write(", "); }
                }
                Console.Write("} ");
            }
            Console.WriteLine("");
        }


    }

    #endregion


}