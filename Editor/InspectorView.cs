using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public class InspectorView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits> { }

        public InspectorView()
        {
        }

        internal void UpdateSelection(SerializedBehaviourTree serializer, NodeView nodeView)
        {
            Clear();

            if (nodeView == null)
            {
                return;
            }

            var nodeProperty = serializer.FindNode(serializer.Nodes, nodeView.node);
            if (nodeProperty == null)
            {
                return;
            }

            // Auto-expand the property
            nodeProperty.isExpanded = true;

            // Property field
            PropertyField field = new PropertyField();
#if UNITY_2021_3_OR_NEWER
            field.label = nodeProperty.managedReferenceValue.GetType().ToString();
#else
            field.label = EditorUtility.GetTargetObjectOfProperty(nodeProperty).GetType().ToString();            
#endif
            field.BindProperty(nodeProperty);

            Add(field);
        }
    }
}
