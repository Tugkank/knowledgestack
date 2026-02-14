using UnityEngine;
using UnityEditor;

namespace KnowledgeStack.Editor
{
    [InitializeOnLoad]
    public class HttpConfigurationTool
    {
        static HttpConfigurationTool()
        {
            // Auto-run on load to ensure dev environment is ready
            EnableHttp();
        }

        [MenuItem("Tools/Knowledge Stack/Enable HTTP Traffic")]
        public static void EnableHttp()
        {
            if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
            {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                Debug.Log("KnowledgeStack: HTTP Traffic Enabled (Cleartext Allowed) in Player Settings.");
            }
        }
    }
}
