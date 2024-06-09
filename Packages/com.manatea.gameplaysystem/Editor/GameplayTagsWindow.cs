using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Manatea.GameplaySystem
{
    internal class GameplayTagsWindow : EditorWindow
    {
        [SerializeField]
        private TreeViewState m_TreeViewState;

        public static GameplayTagTreeView m_TreeView;

        public static SearchField m_SearchField;

        public static bool refresh;

        [MenuItem("Manatea/Gameplay System/Gameplay Tags")]
        public static void ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(GameplayTagsWindow));
            window.titleContent = EditorGUIUtility.TrTextContent("Gameplay Tags");
        }

        private void OnEnable()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new GameplayTagTreeView(m_TreeViewState);

            m_SearchField = new SearchField();
        }
        private void OnDisable()
        {
            m_TreeView = null;
        }

        private void OnGUI()
        {
            // Draw toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Create Tag...", EditorStyles.toolbarButton)) CreateTag();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) ReloadTags();

                string newSearch = m_SearchField.OnToolbarGUI(m_TreeViewState.searchString);
                if (newSearch != m_TreeViewState.searchString)
                {
                    m_TreeViewState.searchString = newSearch;
                    m_TreeView.Reload();
                }
            }
            GUILayout.EndHorizontal();

            // Draw tree view
            Rect treeRect = new Rect(0, 0, position.width, position.height);
            float toolbarHeight = EditorStyles.toolbar.CalcSize(GUIContent.none).y;
            treeRect.y += toolbarHeight;
            treeRect.height -= toolbarHeight;

            m_TreeView.OnGUI(treeRect);
        }


        private void CreateTag()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Tag", "", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                GameplayTag newTag = ScriptableObject.CreateInstance<GameplayTag>();
                AssetDatabase.CreateAsset(newTag, path);
            }
        }

        private void ReloadTags()
        {
            m_TreeView.Reload();
        }
    }

    internal class GameplayTagTreeView : TreeView
    {
        public GameplayTagTreeView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            Reload();
        }


        protected override TreeViewItem BuildRoot()
        {
            // TODO this tree gets rebuild every time we do anything with tags, which can be slow.
            //      Rather try to keep the data and the view manually in sync.

            string[] paths = AssetDatabase.FindAssets("t:" + nameof(GameplayTag));
            List<GameplayTag> gameplayTags = new List<GameplayTag>();
            foreach (string p in paths)
                gameplayTags.Add(AssetDatabase.LoadAssetAtPath<GameplayTag>(AssetDatabase.GUIDToAssetPath(p)));
            foreach (GameplayTag gt in gameplayTags)
                gt.OnValidate();

            TreeViewItem root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            Dictionary<GameplayTag, TreeViewItem> mappingGameplay = new Dictionary<GameplayTag, TreeViewItem>();
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                GameplayTagItem item = new GameplayTagItem { id = i + 1, depth = 0, gameplayTag = gameplayTags[i] };
                mappingGameplay.Add(gameplayTags[i], item);
            }
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                GameplayTag gt = gameplayTags[i];
                if (gt.Parent)
                    mappingGameplay[gt.Parent].AddChild(mappingGameplay[gt]);
                else
                    root.AddChild(mappingGameplay[gt]);
            }

            SetupDepthsFromParentsAndChildren(root);

            // TreeView settings
            showAlternatingRowBackgrounds = true;

            return root;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            string[] keywords = search.Split(' ');
            foreach (string keyword in keywords)
                if (!string.IsNullOrEmpty(keyword) && !base.DoesItemMatchSearch(item, keyword))
                    return false;
            return true;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            // Deselect item
            if ((Event.current.type == EventType.MouseDown && Event.current.button == 0) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                SetSelection(new int[] { }, TreeViewSelectionOptions.FireSelectionChanged);
                Event.current.Use();
            }

            // Duplicate item
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.D)
            {
                DuplicateTag();
                Event.current.Use();
            }

            // Delete item
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            {
                DeleteTag();
                Event.current.Use();
            }
        }

        // Context menu
        protected override void ContextClickedItem(int id)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create Tag..."), false, CreateTag);
            menu.AddItem(new GUIContent("Select Asset"), false, SelectTagAssets);
            if (GetSelection().Count == 1)
                menu.AddItem(new GUIContent("Rename"), false, RenameTag);
            else
                menu.AddDisabledItem(new GUIContent("Rename"));
            menu.AddItem(new GUIContent("Duplicate"), false, DuplicateTag);
            menu.AddItem(new GUIContent("Delete"), false, DeleteTag);
            menu.ShowAsContext();

            //Event.current.Use();
        }
        public void CreateTag()
        {
            string assetPath = "";
            if (GetSelection().Count == 1)
            {
                assetPath = AssetDatabase.GetAssetPath((FindItem(GetSelection()[0], rootItem) as GameplayTagItem).gameplayTag);
                assetPath = assetPath.Remove(assetPath.LastIndexOf('/'));
            }
            string path = EditorUtility.SaveFilePanelInProject("Create Tag", "", "asset", "", assetPath);
            if (!string.IsNullOrEmpty(path))
            {
                GameplayTag newTag = ScriptableObject.CreateInstance<GameplayTag>();
                var selection = GetSelection();
                if (selection.Count > 0)
                    newTag.SetParent((FindItem(selection[0], rootItem) as GameplayTagItem).gameplayTag);
                AssetDatabase.CreateAsset(newTag, path);
            }
        }
        public void SelectTagAssets()
        {
            var selectedTags = GetSelection().Select(i => (FindItem(i, rootItem) as GameplayTagItem).gameplayTag);
            Selection.objects = selectedTags.ToArray();
        }
        public void DuplicateTag()
        {
            var selectedTags = GetSelection().Select(i => (FindItem(i, rootItem) as GameplayTagItem).gameplayTag);
            foreach (var tag in selectedTags)
            {
                GameplayTag newTag = ScriptableObject.Instantiate(tag);
                string newPath = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(tag));
                AssetDatabase.CreateAsset(newTag, newPath);
            }
        }
        public void DeleteTag()
        {
            if (EditorUtility.DisplayDialog("Permanently Delete Tags?", "Do you really want to permanently delete these tags?", "Delete Tags", "Cancel"))
            {
                var selectedAssets = GetSelection().Select(i => AssetDatabase.GetAssetPath((FindItem(i, rootItem) as GameplayTagItem).gameplayTag)).ToArray();
                List<string> failedDeletions = new List<string>();
                AssetDatabase.DeleteAssets(selectedAssets, failedDeletions);
                foreach (string failed in failedDeletions)
                {
                    Debug.LogError("Could not delete " + failed);
                }
            }
        }
        public void RenameTag()
        {
            var selection = GetSelection();
            if (selection.Count > 0)
            {
                BeginRename(FindItem(selection[0], rootItem));
            }
        }


        // Renaming
        protected override bool CanRename(TreeViewItem item) => true;
        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.acceptedRename)
            {
                GameplayTagItem item = (GameplayTagItem)FindItem(args.itemID, rootItem);
                EditorUtility.SetDirty(item.gameplayTag);
                AssetDatabase.SaveAssets();
                string message = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(item.gameplayTag), args.newName);
                if (!string.IsNullOrEmpty(message))
                {
                    Debug.LogError(message);
                    return;
                }
            }
        }

        // Drag & Drop
        protected override bool CanStartDrag(CanStartDragArgs args) => true;

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
            DragAndDrop.SetGenericData("TreeViewItems", draggedRows);
            DragAndDrop.objectReferences = draggedRows.Select(r => (r as GameplayTagItem).gameplayTag).ToArray();
            string title = draggedRows.Count == 1 ? draggedRows[0].displayName : "< Multiple >";
            DragAndDrop.StartDrag(title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            // TODO undo does not work for drag and drop
            if (Event.current.type == EventType.DragPerform)
            {
                var draggedItems = (List<TreeViewItem>)DragAndDrop.GetGenericData("TreeViewItems");
                foreach (var item in draggedItems)
                {
                    if (item == args.parentItem)
                        continue;
                    if (args.parentItem == null || args.parentItem == rootItem)
                    {
                        (item as GameplayTagItem).gameplayTag.SetParent(null);
                    }
                    else
                    {
                        (item as GameplayTagItem).gameplayTag.SetParent((args.parentItem as GameplayTagItem).gameplayTag);
                    }
                    EditorUtility.SetDirty((item as GameplayTagItem).gameplayTag);
                }
                Reload();
            }

            return DragAndDropVisualMode.Link;
        }
    }

    public class GameplayTagItem : TreeViewItem
    {
        public GameplayTag gameplayTag;

        public override string displayName
        {
            get => gameplayTag.name; 
            set => base.displayName = value;
        }
    }

    public class GameplayTagPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            GameplayTagsWindow.m_TreeView?.Reload();
        }
    }
}
