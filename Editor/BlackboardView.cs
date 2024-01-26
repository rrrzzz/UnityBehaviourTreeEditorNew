using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public class BlackboardView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BlackboardView, VisualElement.UxmlTraits>{}

        private SerializedBehaviourTree behaviourTree;
        
        private ListView listView;
        private TextField newKeyTextField;
        private PopupField<Type> newKeyTypeField;

        private Button createButton;
        private bool isDeleteKeyDown;
        private List<string> selectedItemsNames = new List<string>();
        
#if !UNITY_2021_3_OR_NEWER
        private SerializedProperty selectedItem;
        private SerializedObjectList _dataList;
        private SerializedProperty _listProperty;
        private VisualElement selectedVisualElement;
        private VisualElement prevSelectedItem;
        
        private StyleColor prevSelectedBgColor;
        private readonly StyleColor lostFocusSelectedColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
        private readonly StyleColor selectedBgColor = new StyleColor(new Color(0.172549f, 0.3647059f, 0.5294118f));
        
        private bool isFocusLost;
        private bool isSelectionEnabled = true;
#endif

        internal void Bind(SerializedBehaviourTree behaviourTree)
        {
            this.behaviourTree = behaviourTree;
            listView = this.Q<ListView>("ListView_Keys");
            newKeyTextField = this.Q<TextField>("TextField_KeyName");
            VisualElement popupContainer = this.Q<VisualElement>("PopupField_Placeholder");
            createButton = this.Q<Button>("Button_KeyCreate");
            
#if !UNITY_2021_3_OR_NEWER
            listView.UnregisterCallback<FocusOutEvent>(FocusOutHandler);
            listView.RegisterCallback<FocusOutEvent>(FocusOutHandler);
            
            listView.UnregisterCallback<FocusInEvent>(FocusInHandler);
            listView.RegisterCallback<FocusInEvent>(FocusInHandler);

            ClearSelectedItem();
            _listProperty = behaviourTree.BlackboardKeys;
            VisualElement scrollView = this.Q<VisualElement>("unity-content-container");
            scrollView.style.height = new StyleLength(StyleKeyword.Auto);
            scrollView.RegisterCallback<GeometryChangedEvent>(_ => scrollView.style.height = new StyleLength(StyleKeyword.Auto));
            
            listView.selectionType = SelectionType.None;
            BindKeysToListView();
#else
            listView.Bind(behaviourTree.serializedObject);
#endif
            listView.UnregisterCallback<KeyDownEvent>(DeleteSelectedItem);
            listView.RegisterCallback<KeyDownEvent>(DeleteSelectedItem);
            
            listView.UnregisterCallback<KeyUpEvent>(DeleteKeyUpHandler);
            listView.RegisterCallback<KeyUpEvent>(DeleteKeyUpHandler);
            
            newKeyTypeField = new PopupField<Type>();
            newKeyTypeField.label = "Type";
            newKeyTypeField.formatListItemCallback = FormatItem;
            newKeyTypeField.formatSelectedValueCallback = FormatItem;

            var types = TypeCache.GetTypesDerivedFrom<BlackboardKey>();
            foreach (var type in types)
            {
                if (type.IsGenericType)
                {
                    continue;
                }
#if !UNITY_2021_3_OR_NEWER && UNITY_2021
                var prop = newKeyTypeField.GetType().GetField("m_Choices", System.Reflection.BindingFlags.NonPublic
                                                                           | System.Reflection.BindingFlags.Instance);
                var choices = prop.GetValue(newKeyTypeField) as List<Type>;
                choices.Add(type);
                prop.SetValue(newKeyTypeField, choices);
#else
                newKeyTypeField.choices.Add(type);
#endif
                if (newKeyTypeField.value == null)
                {
                    newKeyTypeField.value = type;
                }
            }

            popupContainer.Clear();
            popupContainer.Add(newKeyTypeField);

            // TextField
            newKeyTextField.RegisterCallback<ChangeEvent<string>>((evt) => { ValidateButton(); });
            newKeyTextField.value = "";
            
            // Button
            createButton.clicked -= CreateNewKey;
            createButton.clicked += CreateNewKey;
            
                        
            behaviourTree.OnDeleteBlackboardKey -= RefreshListView;
            behaviourTree.OnDeleteBlackboardKey += RefreshListView;

            ValidateButton();
        }
        
        private void DeleteSelectedItem(KeyDownEvent ev)
        {
#if !UNITY_2021_3_OR_NEWER
            if (isDeleteKeyDown || ev.keyCode != KeyCode.Delete || selectedItem == null)
                    return;

            isDeleteKeyDown = true;
            
            behaviourTree.DeleteBlackboardKey(selectedItem.displayName);
            selectedItem = null;

#else
            if (isDeleteKeyDown || ev.keyCode != KeyCode.Delete || listView.selectedItems == null) 
                return;

            selectedItemsNames.Clear();
            
            foreach (var item in listView.selectedItems)
            {
                var keyProperty = item as SerializedProperty;
                selectedItemsNames.Add(keyProperty.displayName);
            }

            if (selectedItemsNames.Count == 1)
            {
                behaviourTree.DeleteBlackboardKey(selectedItemsNames[0]);
            }
            else
            {
                behaviourTree.DeleteBlackboardKeys(selectedItemsNames);
            }
            
            isDeleteKeyDown = true;
            listView.ClearSelection();
#endif
        }
        
        private void DeleteKeyUpHandler(KeyUpEvent ev)
        {
            if (ev.keyCode != KeyCode.Delete) 
                return;

            isDeleteKeyDown = false;
        }

#if !UNITY_2021_3_OR_NEWER
        private void FocusOutHandler(FocusOutEvent ev)
        {
            isFocusLost = true;
                
            listView.schedule.Execute(() =>
            {
                if (selectedItem != null && isFocusLost)
                {
                    selectedVisualElement.style.backgroundColor = lostFocusSelectedColor;
                }
            }).StartingIn(100);
        }
        
        private void FocusInHandler(FocusInEvent ev)
        {
            isFocusLost = false;
            listView.schedule.Execute(() =>
            {
                if (selectedItem != null)
                {
                    selectedVisualElement.style.backgroundColor = selectedBgColor;
                }
            }).StartingIn(100);
        }
#endif
        
        private string FormatItem(Type arg)
        {
            if (arg == null)
            {
                return "(null)";
            }

            return arg.Name.Replace("Key", "");
        }

        private void ValidateButton()
        {
            // Disable the create button if trying to create a non-unique key
            bool isValidKeyText = ValidateKeyText(newKeyTextField.text);
            createButton.SetEnabled(isValidKeyText);
        }

        bool ValidateKeyText(string text)
        {
            if (text == "")
            {
                return false;
            }

            BehaviourTree tree = behaviourTree.Blackboard.serializedObject.targetObject as BehaviourTree;
            bool keyExists = tree.blackboard.Find(newKeyTextField.text) != null;
            return !keyExists;
        }

        void CreateNewKey()
        {
            Type newKeyType = newKeyTypeField.value;
            if (newKeyType == null) return;
            behaviourTree.CreateBlackboardKey(newKeyTextField.text, newKeyType);
            newKeyTextField.value = "";
            RefreshListView();
        }

        public void ClearView()
        {
            if (behaviourTree != null)
            {
                behaviourTree.OnDeleteBlackboardKey -= RefreshListView;
            }

            behaviourTree = null;

            listView?.UnregisterCallback<KeyDownEvent>(DeleteSelectedItem);
            listView?.UnregisterCallback<KeyUpEvent>(DeleteKeyUpHandler);

            if (listView != null)
            {
#if !UNITY_2021_3_OR_NEWER  
                listView.UnregisterCallback<FocusInEvent>(FocusInHandler);
                listView.UnregisterCallback<FocusOutEvent>(FocusOutHandler);
#elif UNITY_2022_1_OR_NEWER
                listView.ClearSelection();
#else
                listView.Rebuild();
#endif
                listView.Unbind();
            }
        }
        
#if !UNITY_2021_3_OR_NEWER
        public void EnableSelection()
        {
            isSelectionEnabled = true;
        }
        
        public void DisableSelection()
        {
            isSelectionEnabled = false;
            if (selectedItem == null)
            {
                return;
            }
            selectedVisualElement.style.backgroundColor = prevSelectedBgColor;
            ClearSelectedItem();
        }

        private void ClearSelectedItem()
        {
            prevSelectedItem = null;
            selectedItem = null;
            selectedVisualElement = null;
        }
        
        private void BindKeysToListView()
        {
            _dataList = new SerializedObjectList(_listProperty);

            listView.makeItem ??= () => new PropertyField();

            listView.bindItem ??= BindListViewItem;

            listView.itemsSource = _dataList;
        }

        void BindListViewItem(VisualElement ve, int index)
        {
            var field = ve as IBindable;
            if (field == null)
            {
                //we find the first Bindable
                field = ve.Query().Where(x => x is IBindable).First() as IBindable;
            }
            
            if (field == null)
            {
                //can't default bind to anything!
                throw new InvalidOperationException(
                    "Can't find BindableElement: please provide BindableVisualElements or provide your own Listview.bindItem callback");
            }

            object item = listView.itemsSource[index];
            var itemProp = item as SerializedProperty;
            field.bindingPath = itemProp.propertyPath;
            
            ve.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (isFocusLost && ve == selectedVisualElement)
                {
                    ve.style.backgroundColor = selectedBgColor; 
                }
            });
            
            ve.RegisterCallback<MouseOutEvent>(_ =>
            {
                if (isFocusLost && ve == selectedVisualElement)
                {
                    ve.style.backgroundColor = lostFocusSelectedColor; 
                }
            });
            
            ve.RegisterCallback<ClickEvent>(evt =>
            {
                if (!isSelectionEnabled)
                {
                    return;
                }
                
                if (prevSelectedItem == null)
                {
                    prevSelectedItem = ve;
                    prevSelectedBgColor = ve.style.backgroundColor;
                }
                else
                {
                    prevSelectedItem.style.backgroundColor = prevSelectedBgColor;
                    prevSelectedItem = ve;
                    prevSelectedBgColor = ve.style.backgroundColor;
                }
                
                ve.style.backgroundColor = selectedBgColor;
                selectedVisualElement = ve;
                selectedItem = itemProp;
            });
            
            ve.style.height = new StyleLength(StyleKeyword.Auto);
            ve.Bind(itemProp.serializedObject);
        }
#endif
        
        public void RefreshListView()
        {
            ValidateButton();
#if !UNITY_2021_3_OR_NEWER
            _dataList.RefreshProperties(_listProperty);
            listView.Refresh();
#elif UNITY_2021
            listView.RefreshItems();
#endif
#if UNITY_2022_1_OR_NEWER
            listView.ClearSelection();
#endif
        }

        public void ClearSelection()
        {
            listView.ClearSelection();
        }
    }

#if !UNITY_2021_3_OR_NEWER
    internal class SerializedObjectList : IList
    {
        private List<SerializedProperty> properties;

        public SerializedObjectList(SerializedProperty parentProperty)
        {
            RefreshProperties(parentProperty);
        }

        public void RefreshProperties(SerializedProperty parentProperty)
        {
            var property = parentProperty.Copy();
            var endProperty = property.GetEndProperty();

            // Expand the first child.
            property.NextVisible(true); 

            properties = new List<SerializedProperty>();
            do
            {
                if (SerializedProperty.EqualContents(property, endProperty))
                    break;

                if (property.propertyType != SerializedPropertyType.ArraySize)
                {
                    properties.Add(property.Copy());
                }
            } 
            // Never expand children.
            while (property.NextVisible(false)); 
        }

        public object this[int index]
        {
            get { return properties[index]; }
            set { throw new NotImplementedException(); }
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int Count => properties.Count;

        bool ICollection.IsSynchronized
        {
            get { return (properties as ICollection).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return (properties as ICollection).SyncRoot; }
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return IndexOf(value) >= 0;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            var prop = value as SerializedProperty;

            if (value != null && prop != null)
            {
                return properties.IndexOf(prop);
            }

            return -1;
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }
    }
#endif
}
