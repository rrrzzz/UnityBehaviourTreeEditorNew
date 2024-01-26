using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public class NodePort : Port
    {
        // GITHUB:UnityCsReference-master\UnityCsReference-master\Modules\GraphViewEditor\Elements\Port.cs
        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange _graphViewChange;
            private List<Edge> _edgesToCreate;
            private List<GraphElement> _edgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                _edgesToCreate = new List<Edge>();
                _edgesToDelete = new List<GraphElement>();

                _graphViewChange.edgesToCreate = _edgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                NodeView nodeSource = null;
                bool isSourceParent = false;
                if (edge.output != null)
                {
                    nodeSource = edge.output.node as NodeView;
                    isSourceParent = true;
                }
                if (edge.input != null)
                {
                    nodeSource = edge.input.node as NodeView;
                    isSourceParent = false;
                }
                CreateNodeWindow.Show(position, nodeSource, isSourceParent);
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                _edgesToCreate.Clear();
                _edgesToCreate.Add(edge);

                // We can't just add these edges to delete to the m_GraphViewChange
                // because we want the proper deletion code in GraphView to also
                // be called. Of course, that code (in DeleteElements) also
                // sends a GraphViewChange.
                _edgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                    foreach (Edge edgeToDelete in edge.input.connections)
                        if (edgeToDelete != edge)
                            _edgesToDelete.Add(edgeToDelete);
                if (edge.output.capacity == Capacity.Single)
                    foreach (Edge edgeToDelete in edge.output.connections)
                        if (edgeToDelete != edge)
                            _edgesToDelete.Add(edgeToDelete);
                if (_edgesToDelete.Count > 0)
                    graphView.DeleteElements(_edgesToDelete);

                var edgesToCreate = _edgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(_graphViewChange).edgesToCreate;
                }

                foreach (Edge e in edgesToCreate)
                {
                    graphView.AddElement(e);
                    edge.input.Connect(e);
                    edge.output.Connect(e);
                }
            }
        }

        public NodePort(Direction direction, Capacity capacity) : base(Orientation.Vertical, direction, capacity, typeof(bool))
        {
            var connectorListener = new DefaultEdgeConnectorListener();
            m_EdgeConnector = new EdgeConnector<Edge>(connectorListener);
            this.AddManipulator(m_EdgeConnector);
            if (direction == Direction.Input)
            {
                style.width = 100;
            }
            else
            {
                style.width = 40;
            }
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            Rect rect = new Rect(0, 0, layout.width, layout.height);
            return rect.Contains(localPoint);
        }
    }
}
