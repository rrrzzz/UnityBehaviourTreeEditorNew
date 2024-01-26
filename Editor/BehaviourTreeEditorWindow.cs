using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        [System.Serializable]
        public class PendingScriptCreate 
        {
            public bool pendingCreate = false;
            public string scriptName = "";
            public string sourceGuid = "";
            public bool isSourceParent = false;
            public Vector2 nodePosition;

            public void Reset()
            {
                pendingCreate = false;
                scriptName = "";
                sourceGuid = "";
                isSourceParent = false;
                nodePosition = Vector2.zero;
            }
        }

#if UNITY_2021_3_OR_NEWER
        public class BehaviourTreeEditorAssetModificationProcessor : AssetModificationProcessor
        {
#else
        public class BehaviourTreeEditorAssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {
#endif
            static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions opt)
            {
                if (HasOpenInstances<BehaviourTreeEditorWindow>())
                {
                    BehaviourTreeEditorWindow wnd = GetWindow<BehaviourTreeEditorWindow>();
                    wnd.ClearIfSelected(path);
                }
                return AssetDeleteResult.DidNotDelete;
            }
        }
        public static BehaviourTreeEditorWindow Instance;
        public BehaviourTreeProjectSettings settings;
        public VisualTreeAsset behaviourTreeXml;
        public VisualTreeAsset nodeXml;
        public StyleSheet behaviourTreeStyle;
        public TextAsset scriptTemplateActionNode;
        public TextAsset scriptTemplateCompositeNode;
        public TextAsset scriptTemplateDecoratorNode;

        public BehaviourTreeView treeView;
        public InspectorView inspectorView;
        public BlackboardView blackboardView;
        public OverlayView overlayView;
        public ToolbarMenu toolbarMenu;
        public NewScriptDialogView newScriptDialog;
        public ToolbarBreadcrumbs breadcrumbs;

        public bool shouldOpenTree = true;

        [SerializeField]
        public PendingScriptCreate pendingScriptCreate = new PendingScriptCreate();

        [HideInInspector]
        public BehaviourTree tree;
        public SerializedBehaviourTree serializer;
        private bool isSubtreeSelected;

        [MenuItem("Anything World/Behaviour Tree Editor")]
        public static void OpenWindow()
        {
            BehaviourTreeEditorWindow wnd = GetWindow<BehaviourTreeEditorWindow>();
            wnd.overlayView?.Show(false);
            wnd.titleContent = new GUIContent("Behaviour Tree Editor");
            wnd.minSize = new Vector2(800, 600);
        }

        public static void OpenWindow(BehaviourTree tree)
        {
            BehaviourTreeEditorWindow wnd = GetWindow<BehaviourTreeEditorWindow>();
            wnd.titleContent = new GUIContent("Behaviour Tree Editor");
            wnd.minSize = new Vector2(800, 600);
            wnd.SelectNewTree(tree);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if (Instance != null && !Instance.shouldOpenTree)
            {
                Instance.shouldOpenTree = true;
                return false;
            }
            
            if (Selection.activeObject is BehaviourTree) 
            {
                OpenWindow(Selection.activeObject as BehaviourTree);
                return true;
            }
            
            UnityEngine.Object asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset is BehaviourTree behaviourTree)
            {
                OpenWindow(behaviourTree);
                return true;
            }
            
            return false;
        }

#if !UNITY_2021_3_OR_NEWER
        public void EnableBlackboardSelection() => blackboardView.EnableSelection();
        public void DisableBlackboardSelection() => blackboardView.DisableSelection();
#endif
        public void ClearBlackboardSelection() => blackboardView.ClearSelection();

        private void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && 
                serializer != null && overlayView.isShown)
            {
                overlayView.Hide();
            }
        }

        public void CreateGUI()
        {
            Instance = this;
            settings = BehaviourTreeProjectSettings.GetOrCreateSettings();
            
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = behaviourTreeXml;
            visualTree.CloneTree(root);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = behaviourTreeStyle;
            root.styleSheets.Add(styleSheet);

            // Main treeview
            treeView = root.Q<BehaviourTreeView>();
            inspectorView = root.Q<InspectorView>();
            blackboardView = root.Q<BlackboardView>();
            toolbarMenu = root.Q<ToolbarMenu>();
            overlayView = root.Q<OverlayView>("OverlayView");
            newScriptDialog = root.Q<NewScriptDialogView>("NewScriptDialogView");
            breadcrumbs = root.Q<ToolbarBreadcrumbs>("breadcrumbs");

            treeView.styleSheets.Add(behaviourTreeStyle);

            // Toolbar assets menu
            toolbarMenu.RegisterCallback<MouseEnterEvent>((evt) =>
            {
                // Refresh the menu options just before it's opened (on mouse enter)
                toolbarMenu.menu.MenuItems().Clear();
                var behaviourTrees = BehaviourTreeEditorUtility.GetAssetPaths<BehaviourTree>();
                behaviourTrees.ForEach(path =>
                {
                    var fileName = System.IO.Path.GetFileName(path);
                    toolbarMenu.menu.AppendAction($"{fileName}", (a) =>
                    {
                        var tree = AssetDatabase.LoadAssetAtPath<BehaviourTree>(path);
                        SelectNewTree(tree);
                    });
                });
                toolbarMenu.menu.AppendSeparator();
                toolbarMenu.menu.AppendAction("New Tree...", (a) => OnToolbarNewAsset());
            });
            
            treeView.OnNodeSelected -= OnNodeSelectionChanged;
            treeView.OnNodeSelected += OnNodeSelectionChanged;

            // Overlay view
            overlayView.OnTreeSelected -= SelectTree;
            overlayView.OnTreeSelected += SelectTree;

            // New Script Dialog
            newScriptDialog.style.visibility = Visibility.Hidden;

            if (serializer == null)
            {
                overlayView.Show(false);
            }
            else
            {
                SelectTree(serializer.tree);
            }

            // Create new node for any scripts just created coming back from a compile.
            if (pendingScriptCreate != null && pendingScriptCreate.pendingCreate)
            {
                CreatePendingScriptNode();
            }
        }

        void CreatePendingScriptNode()
        {
            NodeView source = treeView.GetNodeByGuid(pendingScriptCreate.sourceGuid) as NodeView;
            var nodeType = Type.GetType($"{pendingScriptCreate.scriptName}, Assembly-CSharp");
            
            var typeName = pendingScriptCreate.scriptName;

            if (nodeType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    nodeType = assembly.GetType(typeName);
                    if (nodeType != null)
                    {
                        break;
                    }
                }
            }

            if (nodeType != null)
            {
                BehaviourTreeEditorUtility.CreateAndSelectNode(source, treeView, nodeType, 
                    pendingScriptCreate.nodePosition, pendingScriptCreate.isSourceParent);
            }

            pendingScriptCreate.Reset();
        }

        void OnUndoRedo()
        {
            if (tree != null)
            {
                serializer.serializedObject.Update();
                treeView.PopulateView(serializer);
                blackboardView?.RefreshListView();
            }
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredEditMode:
                    EditorApplication.delayCall += OnSelectionChange;
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    EditorApplication.delayCall += OnSelectionChange;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    inspectorView?.Clear();
                    break;
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject)
            {
                BehaviourTreeInstanceRunner instanceRunner = Selection.activeGameObject.GetComponent<BehaviourTreeInstanceRunner>();
                if (instanceRunner && instanceRunner.RuntimeTree)
                {
                    SelectNewTree(instanceRunner.RuntimeTree);
                }
            }
        }

        void SelectNewTree(BehaviourTree tree)
        {
            ClearBreadcrumbs();
            SelectTree(tree);
        }

        void SelectTree(BehaviourTree newTree)
        {
            // If tree view is null the window is probably unfocused
            if (treeView == null)
            {
                return;
            }

            if (!newTree)
            {
                ClearBreadcrumbs();
                ClearSelection();
                overlayView.Show(false);
                return;
            }

            if (newTree != tree)
            {
                if (!isSubtreeSelected)
                {
                    ClearBreadcrumbs();
                }

                isSubtreeSelected = false;
                ClearSelection();
            }
            
            tree = newTree;
            serializer = new SerializedBehaviourTree(newTree);

            int childCount = breadcrumbs.childCount;
            breadcrumbs.PushItem($"{serializer.tree.name}", () => PopToSubtree(childCount, newTree));

            overlayView?.Hide();
            treeView?.PopulateView(serializer);
            blackboardView?.Bind(serializer);
        }

        void ClearSelection()
        {
            tree = null;
            serializer = null;
            inspectorView?.Clear();
            treeView?.ClearView();
            blackboardView?.ClearView();
        }

        void ClearIfSelected(string path)
        {
            if (serializer == null)
            {
                return;
            }

            if (AssetDatabase.GetAssetPath(serializer.tree) == path)
            {
                // Need to delay because this is called from a will delete asset callback
                EditorApplication.delayCall += () =>
                {
                    SelectTree(null);
                };
            }
        }

        void OnNodeSelectionChanged(NodeView node)
        {
            inspectorView.UpdateSelection(serializer, node);
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                treeView?.UpdateNodeStates();
            }
        }

        void OnToolbarNewAsset()
        {
            ClearBreadcrumbs();
            overlayView?.Show(true);
        }

        public void PushSubTreeView(SubTree subtreeNode)
        {
            if (subtreeNode.treeAsset != null)
            {
                if (subtreeNode.treeAsset == tree)
                {
                    Debug.LogError("You have assigned subtree equal to the current one. Assign a different subtree to avoid circular reference.");
                    return;
                }
                isSubtreeSelected = true;
                if (Application.isPlaying)
                {
                    SelectTree(subtreeNode.treeInstance);
                }
                else
                {
                    SelectTree(subtreeNode.treeAsset);
                }
            }
            else
            {
                Debug.LogError("No subtree assigned. Assign a behaviour tree to the tree asset field.");
            }
        }

        public void PopToSubtree(int depth, BehaviourTree tree)
        {
            while (breadcrumbs != null && breadcrumbs.childCount > depth)
            {
                breadcrumbs.PopItem();
            }

            isSubtreeSelected = true;
            if (tree)
            {
                SelectTree(tree);
            }
        }

        public void ClearBreadcrumbs()
        {
            while (breadcrumbs != null && breadcrumbs.childCount > 0)
            {
                breadcrumbs.PopItem();
            }
        }
    }
}
