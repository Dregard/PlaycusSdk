using UnityEngine;

namespace Playcus.Utils
{
  public static class AndroidUtils
  {
      // Returns the system architecture
      public static string GetArchitecture()
      {
          using (var system = new AndroidJavaClass("java.lang.System"))
          {
              return system.CallStatic<string>("getProperty", "os.arch");
          }
      }

      public static bool IsArch64()
      {
          if (GetArchitecture().Contains("64"))
          {
              return true;
          }

          return false;
      }
      
      
  }
 
}