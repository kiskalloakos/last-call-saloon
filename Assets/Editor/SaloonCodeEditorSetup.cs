using UnityEditor;
using Unity.CodeEditor;

public static class SaloonCodeEditorSetup
{
    [InitializeOnLoadMethod]
    static void Configure()
    {
        EditorApplication.delayCall += () =>
        {
            if(SessionState.GetBool("Saloon.CodeEditorConfigured", false))return;
            SessionState.SetBool("Saloon.CodeEditorConfigured", true);
            const string editor="/Applications/Visual Studio Code.app";
            if(!System.IO.Directory.Exists(editor))return;
            CodeEditor.SetExternalScriptEditor(editor);
            CodeEditor.CurrentEditor.SyncAll();
        };
    }
}
