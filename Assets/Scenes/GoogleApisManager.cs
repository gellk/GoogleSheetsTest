using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "GoogleApisManager")]
public class GoogleApisManager : ScriptableObject  {
    [SerializeField] private string _sheetId;
    [SerializeField] private string _sheetRange;
    [SerializeField] private string _credentialsJsonPath;
    [SerializeField] static private string[] _sheetScopes = { SheetsService.Scope.SpreadsheetsReadonly };
    [SerializeField] static private string _applicationName = "AppName"; // これはGoogleCouldに定義したものやこのプロジェクト名と別の名前でok(適当な”あああ”とかでも良い)
    static private UserCredential _userCredential = null;

    public void setUserCredential() {
        if (_userCredential != null) {
            Debug.Log("既に_userCredentialが存在する");
            return;
        }

        try {
            // Load client secrets.
            using (var stream = new FileStream(_credentialsJsonPath, FileMode.Open, FileAccess.Read)) {
                /* The file token.json stores the user's access and refresh tokens, and is created
                 automatically when the authorization flow completes for the first time. */
                string credPath = "token.json";
                _userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    _sheetScopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Debug.Log("Credential file saved to : " + credPath);
            }
        } catch (FileNotFoundException e) {
            Debug.Log("getCredential : " + e.Message);
        }
    }

    public IList<IList<object>> getSheetsData() {
        if (_userCredential == null) {
            Debug.Log("_userCredentialが存在しない、Authorizeにより_userCredentialを設定してください");
            return null;
        }
        // Create Google Sheets API service.
        var service = new SheetsService(new BaseClientService.Initializer {
            HttpClientInitializer = _userCredential,
            ApplicationName = _applicationName
        });

        // Define request parameters.
        SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(_sheetId, _sheetRange);

        ValueRange response = request.Execute();
        return response.Values;
    }

    [CustomEditor(typeof(GoogleApisManager))]
    public class DemoInspector : Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            var sheetId = serializedObject.FindProperty("_sheetId");
            var sheetRange = serializedObject.FindProperty("_sheetRange");
            var credentialsJsonPath = serializedObject.FindProperty("_credentialsJsonPath");

            var component = (GoogleApisManager) target;

            using (new GUILayout.VerticalScope(GUI.skin.box)) {
                EditorGUILayout.LabelField("Initialize", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(sheetId);
                EditorGUILayout.PropertyField(sheetRange);
                EditorGUILayout.PropertyField(credentialsJsonPath);

                if (GUILayout.Button("Authorize")) {
                    component.setUserCredential();
                }

                if (GUILayout.Button("Get Sheets Data")) {
                    var sheetsListList = component.getSheetsData();

                    if (sheetsListList == null || sheetsListList.Count == 0) {
                        Debug.Log("No data found.");
                        return;
                    }
                    // とりあえず確認用にログ表示
                    foreach (var row in sheetsListList) {
                        Debug.Log(string.Join(",",  row));
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}