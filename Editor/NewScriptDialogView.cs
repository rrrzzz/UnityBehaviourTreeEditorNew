using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public class NewScriptDialogView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<NewScriptDialogView, UxmlTraits>{}

        private const string DefaultPath = "Assets/";
        BehaviourTreeEditorUtility.ScriptTemplate scriptTemplate;
        TextField textField;
        Button confirmButton;
        NodeView source;
        bool isSourceParent;
        Vector2 nodePosition;

        public void CreateScript(BehaviourTreeEditorUtility.ScriptTemplate scriptTemplate, NodeView source, 
            bool isSourceParent, Vector2 position)
        {
            this.scriptTemplate = scriptTemplate;
            this.source = source;
            this.isSourceParent = isSourceParent;
            this.nodePosition = position;

            style.visibility = Visibility.Visible;

            var background = this.Q<VisualElement>("Background");
            var titleLabel = this.Q<Label>("Title");
            textField = this.Q<TextField>("FileName");
            confirmButton = this.Q<Button>();

            titleLabel.text = $"New {scriptTemplate.subFolder.TrimEnd('s')} Script";

            textField.focusable = true;
            this.RegisterCallback<PointerEnterEvent>(_ =>
            {
                textField[0].Focus();
            });

            textField.RegisterCallback<KeyDownEvent>((e) =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    OnConfirm();
                }
            });

            confirmButton.clicked -= OnConfirm;
            confirmButton.clicked += OnConfirm;

            background.RegisterCallback<PointerDownEvent>((e) =>
            {
                e.StopImmediatePropagation(); 
                Close();
            });
        }

        void Close()
        {
            style.visibility = Visibility.Hidden;
        }

        void OnConfirm()
        {
            string scriptName = textField.text;

            var newNodePath = $"{BehaviourTreeEditorWindow.Instance.settings.newNodePath}";
            if (newNodePath == DefaultPath || AssetDatabase.IsValidFolder(newNodePath))
            {
                var destinationFolder = System.IO.Path.Combine(newNodePath, scriptTemplate.subFolder);
                var destinationPath = System.IO.Path.Combine(destinationFolder, $"{scriptName}.cs");

                System.IO.Directory.CreateDirectory(destinationFolder);

                var parentPath = System.IO.Directory.GetParent(Application.dataPath);

                string templateString = scriptTemplate.templateFile.text;
                templateString = templateString.Replace("#SCRIPTNAME#", scriptName);
                string scriptPath = System.IO.Path.Combine(parentPath.ToString(), destinationPath);

                if (!System.IO.File.Exists(scriptPath))
                {
                    AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
                    System.IO.File.WriteAllText(scriptPath, templateString);

                    // TODO: There must be a better way to survive domain reloads after script compiling than this
                    BehaviourTreeEditorWindow.Instance.pendingScriptCreate.pendingCreate = true;
                    BehaviourTreeEditorWindow.Instance.pendingScriptCreate.scriptName = scriptName;
                    BehaviourTreeEditorWindow.Instance.pendingScriptCreate.nodePosition = nodePosition;
                    if (source != null)
                    {
                        BehaviourTreeEditorWindow.Instance.pendingScriptCreate.sourceGuid = source.node.guid;
                        BehaviourTreeEditorWindow.Instance.pendingScriptCreate.isSourceParent = isSourceParent;
                    }
                    
                    AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceUpdate);
                    confirmButton.SetEnabled(false);
                }
                else
                {
                    Debug.LogError($"Script with that name already exists:{scriptPath}");
                    Close();
                }
            }
            else
            {
                Debug.LogError($"Invalid folder path:{newNodePath}. Check the project configuration settings " +
                               "'newNodePath' is configured to a valid folder");
            }
        }
        
        private void OnAfterAssemblyReload()
        {
            confirmButton.SetEnabled(true);
            Close();
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }
    }
}
