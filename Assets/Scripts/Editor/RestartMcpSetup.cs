using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

// Connect this project once after migration; the package handles later reloads.
[InitializeOnLoad]
internal static class RestartMcpSetup
{
    private const string SetupKey = "Restart.McpSetup.v1";

    static RestartMcpSetup()
    {
        EditorApplication.delayCall += ConnectOnce;
    }

    private static async void ConnectOnce()
    {
        if (Application.isBatchMode || SessionState.GetBool(SetupKey, false)) return;
        SessionState.SetBool(SetupKey, true);
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
        EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
        try
        {
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log(connected ? "[Restart MCP] Connected." : "[Restart MCP] Open Window > MCP for Unity to connect.");
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("[Restart MCP] " + exception.Message);
        }
    }
}
