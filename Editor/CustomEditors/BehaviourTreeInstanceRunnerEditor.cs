using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree 
{
    [CustomEditor(typeof(BehaviourTreeInstanceRunner))]
    public class BehaviourTreeInstanceRunnerEditor : Editor 
    {
        public override VisualElement CreateInspectorGUI() 
        {
            VisualElement container = new VisualElement();

            PropertyField treeField = new PropertyField();
            treeField.bindingPath = nameof(BehaviourTreeInstanceRunner.behaviourTree);

            PropertyField validateField = new PropertyField();
            validateField.bindingPath = nameof(BehaviourTreeInstanceRunner.validate);

            PropertyField publicKeys = new PropertyField();
            publicKeys.bindingPath = nameof(BehaviourTreeInstanceRunner.blackboardOverrides);

            container.Add(treeField);
            container.Add(validateField);
            container.Add(publicKeys);

            return container;
        }
    }
}
