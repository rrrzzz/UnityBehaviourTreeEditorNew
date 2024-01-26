using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public class CreateNodeWindow : ScriptableObject, ISearchWindowProvider
    {
        Texture2D icon;
        BehaviourTreeView treeView;
        NodeView source;
        bool isSourceParent;
        BehaviourTreeEditorUtility.ScriptTemplate[] scriptFileAssets;

        TextAsset GetScriptTemplate(int type)
        {
            var projectSettings = BehaviourTreeProjectSettings.GetOrCreateSettings();

            switch (type)
            {
                case 0:
                    if (projectSettings.scriptTemplateActionNode)
                    {
                        return projectSettings.scriptTemplateActionNode;
                    }
                    return BehaviourTreeEditorWindow.Instance.scriptTemplateActionNode;
                case 1:
                    if (projectSettings.scriptTemplateCompositeNode)
                    {
                        return projectSettings.scriptTemplateCompositeNode;
                    }
                    return BehaviourTreeEditorWindow.Instance.scriptTemplateCompositeNode;
                case 2:
                    if (projectSettings.scriptTemplateDecoratorNode)
                    {
                        return projectSettings.scriptTemplateDecoratorNode;
                    }
                    return BehaviourTreeEditorWindow.Instance.scriptTemplateDecoratorNode;
            }
            Debug.LogError("Unhandled script template type:" + type);
            return null;
        }

        public void Initialise(BehaviourTreeView treeView, NodeView source, bool isSourceParent)
        {
            this.treeView = treeView;
            this.source = source;
            this.isSourceParent = isSourceParent;

            icon = new Texture2D(1, 1);
            icon.SetPixel(0, 0, new Color(0, 0, 0, 0));
            icon.Apply();

            scriptFileAssets = new BehaviourTreeEditorUtility.ScriptTemplate[]
            {
                new BehaviourTreeEditorUtility.ScriptTemplate { templateFile = GetScriptTemplate(0), defaultFileName = "NewActionNode", subFolder = "Actions" },
                new BehaviourTreeEditorUtility.ScriptTemplate { templateFile = GetScriptTemplate(1), defaultFileName = "NewCompositeNode", subFolder = "Composites" },
                new BehaviourTreeEditorUtility.ScriptTemplate { templateFile = GetScriptTemplate(2), defaultFileName = "NewDecoratorNode", subFolder = "Decorators" },
            };
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0)
            };

            // Action nodes can only be added as children
            if (isSourceParent || source == null)
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Actions")) { level = 1 });
                var types = TypeCache.GetTypesDerivedFrom<ActionNode>();
                foreach (var type in types)
                {
                    if (type.IsAbstract)
                    {
                        continue;
                    }

                    AddCreateNodeSearchTreeEntry(tree, type, context);
                }
            }

            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Composites")) { level = 1 });
                {
                    var types = TypeCache.GetTypesDerivedFrom<CompositeNode>();
                    
                    foreach (var type in types)
                    {
                        AddCreateNodeSearchTreeEntry(tree, type, context);
                    }
                }
            }

            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("Decorators")) { level = 1 });
                {
                    var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
                    foreach (var type in types)
                    {
                        AddCreateNodeSearchTreeEntry(tree, type, context);
                    }
                }
            }

            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent("New Script...")) { level = 1 });

                Action createActionScript = () => CreateScript(scriptFileAssets[0], context);
                CreateAndAddSearchTreeEntry(tree, "New Action Script", createActionScript);

                Action createCompositeScript = () => CreateScript(scriptFileAssets[1], context);
                CreateAndAddSearchTreeEntry(tree, "New Composite Script", createCompositeScript);
                
                Action createDecoratorScript = () => CreateScript(scriptFileAssets[2], context);
                CreateAndAddSearchTreeEntry(tree, "New Decorator Script", createDecoratorScript);
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Action invoke = (Action)searchTreeEntry.userData;
            invoke();
            return true;
        }

        public void CreateNode(Type type, SearchWindowContext context)
        {
            BehaviourTreeEditorWindow editorWindow = BehaviourTreeEditorWindow.Instance;
            
            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(
                editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition = editorWindow.treeView.contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeOffset = new Vector2(-75, -20);
            var nodePosition = graphMousePosition + nodeOffset;
            
            BehaviourTreeEditorUtility.CreateAndSelectNode(source, treeView, type, 
                nodePosition, isSourceParent);
        }

        public void CreateScript(BehaviourTreeEditorUtility.ScriptTemplate scriptTemplate, SearchWindowContext context)
        {
            BehaviourTreeEditorWindow editorWindow = BehaviourTreeEditorWindow.Instance;

            var windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(editorWindow.rootVisualElement.parent, context.screenMousePosition - editorWindow.position.position);
            var graphMousePosition = editorWindow.treeView.contentViewContainer.WorldToLocal(windowMousePosition);
            var nodeOffset = new Vector2(-75, -20);
            var nodePosition = graphMousePosition + nodeOffset;

            BehaviourTreeEditorUtility.CreateNewScript(scriptTemplate, source, isSourceParent, nodePosition);
        }

        public static void Show(Vector2 mousePosition, NodeView source, bool isSourceParent = false)
        {
            Vector2 screenPoint = GUIUtility.GUIToScreenPoint(mousePosition);
            CreateNodeWindow searchWindowProvider = ScriptableObject.CreateInstance<CreateNodeWindow>();
            searchWindowProvider.Initialise(BehaviourTreeEditorWindow.Instance.treeView, source, isSourceParent);
            SearchWindowContext windowContext = new SearchWindowContext(screenPoint, 240, 320);
            SearchWindow.Open(windowContext, searchWindowProvider);
        }

        private void AddCreateNodeSearchTreeEntry(List<SearchTreeEntry> tree, Type type, SearchWindowContext context)
        {
            Action invoke = () => CreateNode(type, context);
            CreateAndAddSearchTreeEntry(tree, $"{type.Name}", invoke);
        }
        
        private void CreateAndAddSearchTreeEntry(List<SearchTreeEntry> tree, string guiContentName, Action action) =>
            tree.Add(new SearchTreeEntry(new GUIContent(guiContentName)) { level = 2, userData = action });
    }
}
