using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pchp.CodeAnalysis.Semantics;
using Pchp.CodeAnalysis.Semantics.Graph;
using Pchp.CodeAnalysis.Symbols;

namespace Peachpie.CodeAnalysis.FlowAnalysis.Graph
{
    struct CallSite
    {
        public CallSite(BoundBlock block, BoundRoutineCall callExpression)
        {
            Block = block;
            CallExpression = callExpression;
        }

        BoundBlock Block { get; }

        BoundRoutineCall CallExpression { get; }
    }

    /// <summary>
    /// Stores the information about the calls among the routines in source code. This class is thread-safe.
    /// </summary>
    class CallGraph
    {
        /// <summary>
        /// Maps each node to its incident edges, their directions can be found by <see cref="Edge.Caller"/>
        /// and <see cref="Edge.Callee"/>.
        /// </summary>
        private readonly ConcurrentDictionary<SourceRoutineSymbol, ConcurrentBag<Edge>> _incidentEdges;

        public CallGraph()
        {
            _incidentEdges = new ConcurrentDictionary<SourceRoutineSymbol, ConcurrentBag<Edge>>();
        }

        public Edge AddEdge(SourceRoutineSymbol caller, SourceRoutineSymbol callee, CallSite callSite)
        {
            var edge = new Edge(caller, callee, callSite);
            AddRoutineEdge(caller, edge);
            AddRoutineEdge(callee, edge);

            return edge;
        }

        public IEnumerable<Edge> GetIncidentEdges(SourceRoutineSymbol routine)
        {
            if (_incidentEdges.TryGetValue(routine, out var edges))
            {
                return edges;
            }
            else
            {
                return Array.Empty<Edge>();
            }
        }

        public IEnumerable<Edge> GetCalleeEdges(SourceRoutineSymbol caller)
        {
            return GetIncidentEdges(caller).Where(edge => edge.Caller == caller);
        }

        public IEnumerable<Edge> GetCallerEdges(SourceRoutineSymbol callee)
        {
            return GetIncidentEdges(callee).Where(edge => edge.Callee == callee);
        }

        private void AddRoutineEdge(SourceRoutineSymbol routine, Edge edge)
        {
            _incidentEdges.AddOrUpdate(
                routine,
                _ => new ConcurrentBag<Edge>() { edge },
                (_, edges) => { edges.Add(edge); return edges; });
        }

        public class Edge
        {
            public Edge(SourceRoutineSymbol caller, SourceRoutineSymbol callee, CallSite callSite)
            {
                Caller = caller;
                Callee = callee;
                CallSite = callSite;
            }

            public SourceRoutineSymbol Caller { get; }

            public SourceRoutineSymbol Callee { get; }

            public CallSite CallSite { get; }
        }
    }
}
