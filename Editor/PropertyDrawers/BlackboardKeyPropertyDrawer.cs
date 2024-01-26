using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    [CustomPropertyDrawer(typeof(BlackboardKey))]
    public class BlackboardKeyPropertyDrawer : PropertyDrawer
    {
        private bool _isFieldEventAdded; 
     
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ArraySize)
            {
                _isFieldEventAdded = false;
                
                Label keyName = new Label();
                TextField renameField = new TextField();
                VisualElement container = new VisualElement();
                property.isExpanded = true;

#if UNITY_2021_3_OR_NEWER
                var itemKey = property.managedReferenceValue as BlackboardKey;
#else
                var itemKey = EditorUtility.GetTargetObjectOfProperty(property) as BlackboardKey;
#endif
                
                var keyValue = PropertyDrawerUtils.GetFieldByType(itemKey.underlyingType, nameof(BlackboardKey<object>.value));
           
                keyValue.AddToClassList("hide-label");
                keyValue.style.flexGrow = 1.0f;
                if (!(keyValue is PropertyField))
                {
#if !UNITY_2021_3_OR_NEWER
                    keyValue.RegisterCallback<MouseDownEvent>(_ =>
                    {
                        keyValue.schedule.Execute(() =>
                        {
                            BehaviourTreeEditorWindow.Instance.DisableBlackboardSelection();
                        }).StartingIn(20);
                    });
                        
                    keyValue.RegisterCallback<BlurEvent>(_ => 
                        BehaviourTreeEditorWindow.Instance.EnableBlackboardSelection());
#elif UNITY_2021
                    keyValue.RegisterCallback<MouseDownEvent>(_ =>
                    {
                        BehaviourTreeEditorWindow.Instance.ClearBlackboardSelection();
                    });
#elif UNITY_2022_1_OR_NEWER                   
                    keyValue.RegisterCallback<ClickEvent>(_ =>
                    {
                        BehaviourTreeEditorWindow.Instance.ClearBlackboardSelection();
                    });
#endif
                }
                else
                {
                    keyValue.RegisterCallback<GeometryChangedEvent>(_ =>
                    {
                        if (_isFieldEventAdded) return;

                        var callbackVe = keyValue.Children().First();
                        
#if !UNITY_2021_3_OR_NEWER                    
                        callbackVe.RegisterCallback<MouseDownEvent>(_ =>
                        {
                            callbackVe.schedule.Execute(() =>
                            {
                                BehaviourTreeEditorWindow.Instance.DisableBlackboardSelection();
                            }).StartingIn(20);
                        });
                        
                        callbackVe.RegisterCallback<BlurEvent>(_ => 
                            BehaviourTreeEditorWindow.Instance.EnableBlackboardSelection());
#elif UNITY_2021
                        callbackVe.RegisterCallback<MouseDownEvent>(_ =>
                        {
                            BehaviourTreeEditorWindow.Instance.ClearBlackboardSelection();
                        });
#elif UNITY_2022_1_OR_NEWER                   
                        callbackVe.RegisterCallback<ClickEvent>(_ =>
                        {
                            BehaviourTreeEditorWindow.Instance.ClearBlackboardSelection();
                        });
#endif
                        _isFieldEventAdded = true;
                    });
                }
                
                keyName.bindingPath = nameof(BlackboardKey.name);
                keyName.AddToClassList("unity-base-field__label");
                keyName.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount != 2) return;
#if !UNITY_2021_3_OR_NEWER  
                    BehaviourTreeEditorWindow.Instance.DisableBlackboardSelection();
#else
                    BehaviourTreeEditorWindow.Instance.ClearBlackboardSelection();
#endif
                    renameField.value = keyName.text;
                    renameField.style.display = DisplayStyle.Flex;
                    renameField.Focus();

                    keyValue.style.display = DisplayStyle.None;
                    keyName.style.display = DisplayStyle.None;
                });

                renameField.style.display = DisplayStyle.None;
                renameField.bindingPath = nameof(BlackboardKey.name);
                renameField.RegisterCallback<BlurEvent>(evt =>
                {
#if !UNITY_2021_3_OR_NEWER  
                    BehaviourTreeEditorWindow.Instance.EnableBlackboardSelection();
#endif
                    keyValue.style.display = DisplayStyle.Flex;
                    keyName.style.display = DisplayStyle.Flex;
                    renameField.style.display = DisplayStyle.None;
                });
                
                container.style.flexDirection = FlexDirection.Row;
                container.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Delete", _ => 
                        BehaviourTreeEditorWindow.Instance.serializer.DeleteBlackboardKey(property.displayName), 
                        DropdownMenuAction.AlwaysEnabled);
                }));
                
                container.Add(keyName);
                container.Add(renameField);
                container.Add(keyValue);
                
                return container;
            }
            return null;
        }
    }
}
