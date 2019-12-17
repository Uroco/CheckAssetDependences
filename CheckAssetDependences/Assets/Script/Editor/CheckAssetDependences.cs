using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
public class CheckAssetDependences : EditorWindow
{
    [MenuItem("Window/CheckAssetDependences")]
    public static void OpenWindow()
    {
        EditorWindow.GetWindow<CheckAssetDependences>();
    }
    public enum SearchMode
    {
        Input = 0,
        Refer = 1,
        InFile = 2,
    }

    public enum AssetType
    {
        Texture,
        Material,
        Prefab,
        Script
    }


    SearchMode searchMode = 0;
    List<string> resultStr = new List<string>();
    Vector2 resultScroll = Vector2.zero;
    bool needCollection = true;

    //Input用
    string findName = "";
    AnimBool hasFilterTypes;
    Vector2 foundScroll = Vector2.zero;
    List<FilterToggle> filterToggles;
    string prebFindName;
    List<PathLabelButton> pathLabels = new List<PathLabelButton>();

    //refer
    Object referObj;

    //in folder
    Object folder;
    List<string> searchedPathStr = new List<string>();
    Vector2 searchedScroll = Vector2.zero;

    void Init()
    {
        filterToggles = new List<FilterToggle>();
        filterToggles.Add(new FilterToggle(true, "Textrue", AssetType.Texture));
        filterToggles.Add(new FilterToggle(true, "Material", AssetType.Material));
        filterToggles.Add(new FilterToggle(true, "Prefab", AssetType.Prefab));
        filterToggles.Add(new FilterToggle(true, "Script", AssetType.Script));
        hasFilterTypes = new AnimBool(false);
        hasFilterTypes.valueChanged.AddListener(Repaint);   //EditorWindowのメソッド

        referObj = null;

    }

    void OnEnable()
    {
        if (filterToggles == null)
        {
            Init();
        }

        FilterToggle.isChanged = true;
        prebFindName = "";
        resultStr.Clear();
        needCollection = true;
    }

    //projectの状態が変更されたときよばれる
    void OnProjectChange()
    {
        needCollection = true;
    }

    //表示のリセット
    void InitModes()
    {
        resultStr.Clear();
        findName = "";
        prebFindName = "";
        pathLabels.Clear();

        referObj = null;

        folder = null;
        searchedPathStr.Clear();
    }

    //UI切り替え
    void OnGUI()
    {
        GUILayout.Label("This is the system of checking for using of the assets.", EditorStyles.label);
        var mode = (SearchMode)EditorGUILayout.EnumPopup(new GUIContent("Search Mode"), searchMode, GUILayout.Width(300), GUILayout.Height(20));
        EditorGUILayout.Space();
        if (mode != searchMode)
        {
            InitModes();
        }
        searchMode = mode;
        switch (searchMode)
        {
            case SearchMode.Input:
                ShowInputMode();
                break;
            case SearchMode.Refer:
                ShowReferMode();
                break;
            case SearchMode.InFile:
                ShowInFolderMode();
                break;
        }

        if (resultStr.Count > 0)
        {
            GUILayout.Label("Result", EditorStyles.boldLabel);
            resultScroll = EditorGUILayout.BeginScrollView(resultScroll,
                false,
                true,
                GUILayout.Width(position.width),
                GUILayout.Height(position.height * 0.3f));
            //参照検索結果表示領域
            foreach (var result in resultStr)
            {
                EditorGUILayout.SelectableLabel(result, EditorStyles.label, GUILayout.Height(20));
            }
            EditorGUILayout.EndScrollView();
        }
    }
    #region InputMode

    //トリガークラス
    protected class FilterToggle
    {
        public static bool isChanged = false;

        public AssetType type;
        public bool st;
        public string text;
        public FilterToggle(bool _st, string _text, AssetType _type)
        {
            st = _st;
            text = _text;
            type = _type;
        }
        public void Show()
        {
            var _st = EditorGUILayout.ToggleLeft(text, st, GUILayout.Width(100));
            if (_st != st)
            {
                isChanged = true;
            }
            st = _st;
        }
    }

    //検索モード
    void ShowInputMode()
    {
        GUILayout.Label("1.Input the name you want to search", EditorStyles.label);
        GUILayout.Label("2.Click the file path from Found Assets Area", EditorStyles.label);
        GUILayout.Label("3.Print below on the Result area", EditorStyles.label);

        //検索名入力スペース
        findName = EditorGUILayout.TextField("Asset Name:", findName, GUILayout.Width(position.width * 0.8f));

        //検索タイプフィルタ
        bool refinder = false;
        var hasFilter = EditorGUILayout.ToggleLeft("Filter", hasFilterTypes.target, EditorStyles.boldLabel);
        if (hasFilter != hasFilterTypes.target)
        {
            refinder = true;
        }
        hasFilterTypes.target = hasFilter;

        if (EditorGUILayout.BeginFadeGroup(hasFilterTypes.faded))
        {
            GUILayout.Label("Find Types:", EditorStyles.largeLabel);

            EditorGUILayout.BeginHorizontal();
            foreach (var toggle in filterToggles)
            {
                toggle.Show();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndFadeGroup();
        }

        //検索結果表示領域
        GUILayout.Label("Found Assets", EditorStyles.boldLabel);
        foundScroll = EditorGUILayout.BeginScrollView(foundScroll,
            false,
            true,
            GUILayout.Width(position.width),
            GUILayout.Height(position.height * 0.3f));

        if (prebFindName.Equals(findName)
            && !refinder
            && !FilterToggle.isChanged)
        {
            foreach (var label in pathLabels)
            {
                label.ShowButton();
            }
        }
        else
        {
            CreatePathLabel(findName);
        }
        EditorGUILayout.EndScrollView();
        FilterToggle.isChanged = false;
    }



    string GetTypeString(AssetType type)
    {
        switch (type)
        {
            case AssetType.Texture:
                return "texture2D";
            case AssetType.Material:
                return "material";
            case AssetType.Prefab:
                return "prefab";
            case AssetType.Script:
                return "script";
        }
        return "";
    }


    //検索結果のpath表示（押下可能）
    void CreatePathLabel(string name)
    {

        pathLabels.Clear();

        if (name.Length < 1)
        {
            EditorGUILayout.TextField("Please enter any asset name", EditorStyles.boldLabel);
            return;
        }

        if (hasFilterTypes.value)
        {
            foreach (var filter in filterToggles)
            {
                if (filter.st)
                {
                    EditorGUILayout.TextField("===" + filter.text + "===\n", EditorStyles.boldLabel);
                    FindAndCreateLabel(name, GetTypeString(filter.type));
                }
            }
        }
        else
        {
            FindAndCreateLabel(name);
        }
        prebFindName = name;
    }

    #region labelButton
    protected class PathLabelButton
    {
        public string path;
        public string guid;
        public string type;
        public GUIStyle style;
        public System.Action<PathLabelButton> callback;

        public PathLabelButton(string _path, string _guid, string _type)
        {
            path = _path;
            guid = _guid;
            type = _type;
            style = new GUIStyle();
            style.normal.textColor = Color.white;
        }
        public void SetActive(bool st)
        {
            if (st)
            {
                style.normal.textColor = Color.yellow;
            }
            else
            {
                style.normal.textColor = Color.white;
            }
        }

        public void ShowButton()
        {
            if (GUILayout.Button(path, style, GUILayout.Height(20)))
            {
                callback(this);
            }
        }
    }
    #endregion

    //名前検索してPathLabelButton作成
    void FindAndCreateLabel(string name, string type = "")
    {
        string filter = name;
        if (type != "")
        {
            filter += " t:" + type;
        }
        string[] guids = AssetDatabase.FindAssets(filter);

        if (guids != null && guids.Length > 0)
        {
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var pathLabel = new PathLabelButton(path, guid, type);
                pathLabel.callback = CheckUsing;
                pathLabels.Add(pathLabel);
                pathLabel.ShowButton();
            }
        }
    }

    //path押下時の処理
    void CheckUsing(PathLabelButton pathLabel)
    {
        //検索リストの見た目の更新
        foreach (var label in pathLabels)
        {
            if (!pathLabel.Equals(label))
            {
                label.SetActive(false);
            }
        }
        pathLabel.SetActive(true);
        foundScroll = Vector2.zero;
        CheckUsing(pathLabel.path, pathLabel.guid);
    }
    #endregion

    //参照検索モード
    void ShowReferMode()
    {
        GUILayout.Label("Put the any object you want to search", EditorStyles.label);
        var obj = EditorGUILayout.ObjectField(referObj, typeof(Object), true);
        if (obj != null && !obj.Equals(referObj))
        { //セットされた型のチェック
            var objType = obj.GetType();
            if (objType.Equals(typeof(Texture2D))
                || objType.Equals(typeof(Texture))
                || objType.Equals(typeof(RenderTexture))
                || objType.Equals(typeof(Material))
                || objType.Equals(typeof(GameObject))
                || objType.Equals(typeof(MonoScript))

            )
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string guid = AssetDatabase.AssetPathToGUID(path);
                CheckUsing(path, guid);

            }
            else
            {
                resultStr.Add("It is invalid object.");
                referObj = obj;
                return;
            }


        }
        referObj = obj;
    }

    class AssetsInfo
    {
        public string guid;
        public string path;
        public string[] assetsPathes;

        //pathのAssetを使用しているかどうか
        public bool isUsingTheAsset(string path)
        {
            foreach (var assetsPath in assetsPathes)
            {
                if (assetsPath.Equals(path))
                {
                    return true;        //同一アセットを複数参照していることはないと思うので、ここで打ち止め
                }
            }
            return false;
        }
    }
    //検索対象のものを保存しておく
    Dictionary<string, AssetsInfo> TargetAssets = new Dictionary<string, AssetsInfo>();
    //検索対象を抽出
    void CollectTartgetAssets()
    {
        TargetAssets.Clear();
        string[] targetGuids = AssetDatabase.FindAssets("t:scene t:prefab t:material");
        foreach (var targetGuid in targetGuids)
        {
            //guidから参照しているアセットを取り出す
            var targetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
            var assetsPathes = AssetDatabase.GetDependencies(targetPath);
            var asset = new AssetsInfo();
            asset.guid = targetGuid;
            asset.path = targetPath;
            asset.assetsPathes = assetsPathes;
            TargetAssets.Add(targetGuid, asset);
        }
        needCollection = false;
    }

    //使用されているかチェックして出力
    void CheckUsing(string path, string guid)
    {
        resultScroll = Vector2.zero;
        resultStr.Clear();
        if (needCollection)
        {
            CollectTartgetAssets();
        }
        foreach (var asset in TargetAssets.Values)
        {
            if (!asset.guid.Equals(guid) //検索対象自身なら処理しない
                && asset.isUsingTheAsset(path))
            {
                resultStr.Add(asset.path);
            }
        }

        if (resultStr.Count < 1)
        {
            resultStr.Add("NONE");
        }
    }



    //フォルダ一括検索モード　使われていないものを表示
    void ShowInFolderMode()
    {
        GUILayout.Label("Put the any folder you want to check.", EditorStyles.label);
        GUILayout.Label("Display unused files at the bottom.", EditorStyles.label);
        GUILayout.Label("It might take a time, so I recommend taking coffee break :)", EditorStyles.label);

        var obj = EditorGUILayout.ObjectField(folder, typeof(Object), true);
        if (obj != null && !obj.Equals(folder))
        {
            resultScroll = Vector2.zero;
            searchedScroll = Vector2.zero;
            resultStr.Clear();
            searchedPathStr.Clear();
            if (needCollection)
            {
                CollectTartgetAssets();
            }

            var path = AssetDatabase.GetAssetOrScenePath(obj);
            if (System.IO.Directory.Exists(path))
            {    //フォルダかどうかチェック
                string[] folderGuids = AssetDatabase.FindAssets("t:prefab t:material t:texture t:script", new string[] { path });
                foreach (var guid in folderGuids)
                {
                    var folderPath = AssetDatabase.GUIDToAssetPath(guid);
                    searchedPathStr.Add(folderPath);
                    bool unused = true;
                    foreach (var asset in TargetAssets.Values)
                    {
                        if (asset.guid.Equals(guid))
                        { //検索対象自身なら処理しない
                            continue;
                        }
                        if (asset.isUsingTheAsset(folderPath))
                        {   //何かに使用されていれば検索終了
                            unused = false;
                            break;
                        }
                    }
                    if (unused)
                    {
                        //使われていないんじゃあ。。。
                        resultStr.Add(folderPath);
                    }
                }
                if (resultStr.Count < 1)
                {
                    resultStr.Add("NONE");
                }
            }
            else
            {
                resultStr.Add("It is invalid object."); //設定されたのがフォルダではない
            }
        }
        folder = obj;

        //検索対象表示領域
        GUILayout.Label("Searched Path", EditorStyles.boldLabel);
        searchedScroll = EditorGUILayout.BeginScrollView(searchedScroll,
            false,
            true,
            GUILayout.Width(position.width),
            GUILayout.Height(position.height * 0.3f));
        foreach (var searchedPath in searchedPathStr)
        {
            GUILayout.Label(searchedPath, EditorStyles.label);
        }
        EditorGUILayout.EndScrollView();

    }
}