﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;
using System.Linq;
public class CheckAssetDependences : EditorWindow
{
    [MenuItem("Window/CheckAssetDependences")]
    public static void OpenWindow()
    {
        var window = (CheckAssetDependences)EditorWindow.GetWindow(typeof(CheckAssetDependences), false, "Check Asset Dependences");
    }

    [Serializable]
    private enum Language
    {
        Japanese = 0,
        English
    }

    [Serializable]
    private enum SearchMode
    {
        Input = 0,
        Refer = 1,
        InFolder = 2,
    }

    public enum AssetType
    {
        Texture,
        Material,
        Prefab,
        Script,
        Mesh
    }

    [Serializable]
    private class MenuToggle<T>
    {
        private GUIContent[] toggles = null;
        public GUIContent[] Toggles
        {
            get
            {
                if (toggles == null)
                {
                    toggles = System.Enum.GetNames(typeof(T)).Select(x => new GUIContent(x)).ToArray();
                }
                return toggles;
            }
        }


    }
    [SerializeField] Language language = 0;
    [SerializeField] SearchMode searchMode = 0;
    [SerializeField] MenuToggle<SearchMode> menuToggle = new MenuToggle<SearchMode>();
    [SerializeField] List<string> resultStr = new List<string>();
    [SerializeField] Vector2 resultScroll = Vector2.zero;
    [SerializeField] bool needCollection = true;

    //Input用
    [SerializeField] string findName = "";
    [SerializeField] AnimBool hasFilterTypes;
    [SerializeField] Vector2 foundScroll = Vector2.zero;
    [SerializeField] List<FilterToggle> filterToggles;
    [SerializeField] string prebFindName;
    [SerializeField] List<PathLabelButton> pathLabels = new List<PathLabelButton>();
    [SerializeField] bool exceptPackages = true;

    //refer
    [SerializeField] UnityEngine.Object referObj;

    //in folder
    [SerializeField] UnityEngine.Object folder;
    [SerializeField] List<string> searchedPathStr = new List<string>();
    [SerializeField] Vector2 searchedScroll = Vector2.zero;
    void Init()
    {
        filterToggles = new List<FilterToggle>();
        filterToggles.Add(new FilterToggle(true, "Textrue", AssetType.Texture));
        filterToggles.Add(new FilterToggle(true, "Material", AssetType.Material));
        filterToggles.Add(new FilterToggle(true, "Prefab", AssetType.Prefab));
        filterToggles.Add(new FilterToggle(true, "Script", AssetType.Script));
        filterToggles.Add(new FilterToggle(true, "Mesh", AssetType.Mesh));
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

        if (menuToggle == null)
        {
            menuToggle = new MenuToggle<SearchMode>();
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
        if (pathLabels != null)
        {
            pathLabels.Clear();
        }
        else
        {
            pathLabels = new List<PathLabelButton>();
        }

        referObj = null;

        folder = null;
        searchedPathStr.Clear();
    }

    private string GetText(int id)
    {
        string str = "";
        switch (language)
        {
            case Language.Japanese:
                str = JapaneseVerText(id);
                break;

            case Language.English:
                str = EnglishVerText(id);
                break;
        }

        if (string.IsNullOrEmpty(str))
        {
            str = EnglishVerText(id);
        }
        return str;
    }

    private string EnglishVerText(int id)
    {
        switch (id)
        {
            case 1: return "This is the system of checking for using of the assets.";
            case 2: return "Language";
            case 3: return "Search Mode";
            case 4: return "Result";
            case 10: return "NONE";

            case 100: return "1.Input the name you want to search";
            case 101: return "2.Click the file path from Found Assets Area";
            case 102: return "3.Print below on the Result area";
            case 103: return "Asset Name:";
            case 104: return "Filter";
            case 105: return "Find Types:";
            case 106: return "Found Assets";
            case 107: return "Please enter any asset name";
            case 108: return "except Packages";

            case 200: return "Put the any object you want to search";
            case 201: return "It is invalid object.";

            case 300: return "Put the any folder you want to check.";
            case 301: return "Display unused files at the bottom.";
            case 302: return "It might take a time, so I recommend taking coffee break :)";
            case 303: return "It is invalid object.";
            case 304: return "Searched Path";

        }
        return " ";
    }
    private string JapaneseVerText(int id)
    {
        switch (id)
        {
            case 1: return "使用されているアセットを調べるツールです。";
            case 2: return "言語";
            case 3: return "検索モード";
            case 4: return "結果";
            case 10: return "なし";

            case 100: return "1.調べたいアセット名を入れてください";
            case 101: return "2.見つかったアセットのエリアに表示されたパスをクリックしてください。";
            case 102: return "3.下にクリックされたAssetの使用元が検索され表示されます。";
            case 103: return "アセット名:";
            case 104: return "フィルタ";
            case 105: return "見つけるタイプ:";
            case 106: return "見つかったアセット";
            case 107: return "なにかアセット名を入力してください。";
            case 108: return "パッケージを含めない";


            case 200: return "調べたいアセットを入れてください";
            case 201: return "無効なオブジェクトです。";

            case 300: return "調べたいフォルダを入れてください";
            case 301: return "使用されていないファイルが下に表示されます。";
            case 302: return "もしかしたら時間がかかるかもなので、コーヒーでも飲んでお待ちください（＾v＾）";
            case 303: return "無効なオブジェクトです。";
            case 304: return "検索したパス";
        }
        return " ";
    }


    //UI切り替え
    void OnGUI()
    {
        EditorGUILayout.HelpBox(GetText(1), MessageType.Info);
        language = (Language)EditorGUILayout.EnumPopup(new GUIContent(GetText(2)), language, GUILayout.Width(300));

        var originalBgCol = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            var mode = (SearchMode)GUILayout.Toolbar(
                            (int)searchMode,
                             menuToggle.Toggles);
            GUILayout.FlexibleSpace();

            if (mode != searchMode)
            {
                InitModes();
            }
            searchMode = mode;
        }
        GUI.backgroundColor = originalBgCol;
        EditorGUILayout.Space();

        switch (searchMode)
        {
            case SearchMode.Input:
                ShowInputMode();
                break;
            case SearchMode.Refer:
                ShowReferMode();
                break;
            case SearchMode.InFolder:
                ShowInFolderMode();
                break;
        }

        if (resultStr.Count > 0)
        {
            GUILayout.Label(GetText(4), EditorStyles.boldLabel);
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
    public class FilterToggle
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
        EditorGUILayout.LabelField(GetText(100));
        EditorGUILayout.LabelField(GetText(101));
        EditorGUILayout.LabelField(GetText(102));

        //検索名入力スペース
        findName = EditorGUILayout.TextField(GetText(103), findName, GUILayout.Width(position.width * 0.8f));

        //検索タイプフィルタ
        bool refinder = false;
        var hasFilter = EditorGUILayout.ToggleLeft(GetText(104), hasFilterTypes.target, EditorStyles.boldLabel);
        if (hasFilter != hasFilterTypes.target)
        {
            refinder = true;
        }
        hasFilterTypes.target = hasFilter;

        if (EditorGUILayout.BeginFadeGroup(hasFilterTypes.faded))
        {
            GUILayout.Label(GetText(105), EditorStyles.largeLabel);

            EditorGUILayout.BeginHorizontal();
            foreach (var toggle in filterToggles)
            {
                toggle.Show();
            }

        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndFadeGroup();

        //パッケージ化を含めない
        var ep = EditorGUILayout.ToggleLeft(GetText(108), exceptPackages);
        if (exceptPackages != ep)
        {
            refinder = true;
            exceptPackages = ep;
        }

        //検索結果表示領域
        GUILayout.Label(GetText(106), EditorStyles.boldLabel);
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
            case AssetType.Mesh:
                return "mesh";
        }
        return "";
    }


    //検索結果のpath表示（押下可能）
    void CreatePathLabel(string name)
    {

        pathLabels.Clear();

        if (name.Length < 1)
        {
            EditorGUILayout.TextField(GetText(107), EditorStyles.boldLabel);
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
    [Serializable]
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
                if (exceptPackages
                    && path.StartsWith("Packages/"))
                {
                    continue;
                }
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
        GUILayout.Label(GetText(200), EditorStyles.label);
        var obj = EditorGUILayout.ObjectField(referObj, typeof(UnityEngine.Object), true);
        if (obj != null && !obj.Equals(referObj))
        { //セットされた型のチェック
            var objType = obj.GetType();
            if (objType.Equals(typeof(Texture2D))
                || objType.Equals(typeof(Texture))
                || objType.Equals(typeof(RenderTexture))
                || objType.Equals(typeof(Material))
                || objType.Equals(typeof(GameObject))
                || objType.Equals(typeof(MonoScript))
                || objType.Equals(typeof(Mesh))
            )
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string guid = AssetDatabase.AssetPathToGUID(path);
                CheckUsing(path, guid);

            }
            else
            {
                resultStr.Add(GetText(201));
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
            resultStr.Add(GetText(10));
        }
    }



    //フォルダ一括検索モード　使われていないものを表示
    void ShowInFolderMode()
    {
        GUILayout.Label(GetText(300), EditorStyles.label);
        GUILayout.Label(GetText(301), EditorStyles.label);
        GUILayout.Label(GetText(302), EditorStyles.label);

        var obj = EditorGUILayout.ObjectField(folder, typeof(UnityEngine.Object), false);
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
                folder = obj;

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
                    resultStr.Add(GetText(10));
                }
            }
            else
            {
                resultStr.Add(GetText(303)); //設定されたのがフォルダではない
                folder = null;
            }
        }

        //検索対象表示領域
        GUILayout.Label(GetText(304), EditorStyles.boldLabel);
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