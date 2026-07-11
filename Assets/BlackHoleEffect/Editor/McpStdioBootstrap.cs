using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;

namespace BlackHoleEffect.Editor
{
    /// <summary>
    /// Ensures the MCP for Unity bridge runs in stdio mode and is started, so
    /// external MCP clients (which discover instances via ~/.unity-mcp status
    /// files) can connect without opening the MCP window manually.
    /// </summary>
    public static class McpStdioBootstrap
    {
        [InitializeOnLoadMethod]
        static void EnsureStdioBridge()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    if (EditorConfigurationCache.Instance.UseHttpTransport)
                        EditorConfigurationCache.Instance.SetUseHttpTransport(false);
                    StdioBridgeHost.Start();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning("MCP stdio bootstrap failed: " + e.Message);
                }
            };
        }
    }
}
