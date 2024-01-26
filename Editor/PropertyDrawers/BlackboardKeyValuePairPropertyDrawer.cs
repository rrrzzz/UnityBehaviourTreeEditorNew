using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree 
{
    [CustomPropertyDrawer(typeof(BlackboardKeyValuePair))]
    public class BlackboardKeyValuePairPropertyDrawer : PropertyDrawer
    {
        VisualElement pairContainer;

        BehaviourTree GetBehaviourTree(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is BehaviourTree tree)
            {
                return tree;
            }
            else if (property.serializedObject.targetObject is BehaviourTreeInstanceRunner instance)
            {
                return instance.RuntimeTree;
            }
            Debug.LogError("Could not find behaviour tree this is referencing");
            return null;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty first = property.FindPropertyRelative(nameof(BlackboardKeyValuePair.key));
            SerializedProperty second = property.FindPropertyRelative(nameof(BlackboardKeyValuePair.value));

            PopupField<BlackboardKey> dropdown = new PopupField<BlackboardKey>();
            dropdown.label = first.displayName;
            dropdown.formatListItemCallback = FormatItem;
            dropdown.formatSelectedValueCallback = FormatItem;
            
#if UNITY_2021_3_OR_NEWER
            dropdown.value = first.managedReferenceValue as BlackboardKey;
#else
            dropdown.value = EditorUtility.GetTargetObjectOfProperty(first) as BlackboardKey;
#endif
            
            BehaviourTree tree = GetBehaviourTree(property);
            
            dropdown.RegisterCallback<MouseEnterEvent>((evt) =>
            {
#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = dropdown.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                    | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(dropdown) as List<BlackboardKey>;
                    choices.Clear();
                    foreach (var key in tree.blackboard.keys)
                    {
                        choices.Add(key);
                    }
                prop.SetValue(dropdown, choices);
#else
                dropdown.choices.Clear();
                foreach (var key in tree.blackboard.keys)
                {
                    dropdown.choices.Add(key);
                }
#endif
            });

            dropdown.RegisterCallback<ChangeEvent<BlackboardKey>>((evt) =>
            {
                BlackboardKey newKey = evt.newValue;
                first.managedReferenceValue = newKey;
                property.serializedObject.ApplyModifiedProperties();

                if (pairContainer.childCount > 1)
                {
                    pairContainer.RemoveAt(1);
                }

#if UNITY_2021_3_OR_NEWER
                var secondManagedReferenceValue = second.managedReferenceValue;
#else
                var secondManagedReferenceValue = EditorUtility.GetTargetObjectOfProperty(second);
#endif
                if (secondManagedReferenceValue == null || secondManagedReferenceValue.GetType() != dropdown.value.GetType())
                {
                    second.managedReferenceValue = BlackboardKey.CreateKey(dropdown.value.GetType());
                    second.serializedObject.ApplyModifiedProperties();
                }
                PropertyField field = new PropertyField();
                field.label = second.displayName;
                field.BindProperty(second.FindPropertyRelative(nameof(BlackboardKey<object>.value)));
                pairContainer.Add(field);
            });

            pairContainer = new VisualElement();
            pairContainer.Add(dropdown);

#if UNITY_2021_3_OR_NEWER
            var secondManagedReferenceValue = second.managedReferenceValue;
#else
            var secondManagedReferenceValue = EditorUtility.GetTargetObjectOfProperty(second);
#endif
            
            if (dropdown.value != null)
            {
                if (secondManagedReferenceValue == null ||
                    
#if UNITY_2021_3_OR_NEWER
                    first.managedReferenceValue.GetType()
#else
                    EditorUtility.GetTargetObjectOfProperty(first).GetType() 
#endif
                    != secondManagedReferenceValue.GetType())
                    {
                    second.managedReferenceValue = BlackboardKey.CreateKey(dropdown.value.GetType());
                    second.serializedObject.ApplyModifiedProperties();
                }

                PropertyField field = new PropertyField();
                field.label = second.displayName;
                field.bindingPath = nameof(BlackboardKey<object>.value);
                pairContainer.Add(field);
            }

            return pairContainer;
        }

        private string FormatItem(BlackboardKey item)
        {
            if (item == null)
            {
                return "(null)";
            }
            else
            {
                return item.name;
            }
        }
    }
}
