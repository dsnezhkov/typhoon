using System;
using System.Collections.Generic;

namespace Cradle
{
    internal static class ConfigUtil
    {

        public static Dictionary<String, String> CONFIG = new Dictionary<String, String>();
        public static bool DEBUG = false;

        public static Dictionary<String, String> GetConfig()
        {
            return CONFIG;
        }
        public static void PrintConfig()
        {
            foreach (KeyValuePair<String, String> configDict in CONFIG)
            {
                Console.WriteLine("{0} : {1}", configDict.Key, configDict.Value);
            }
        }

        public static String GetConfigSetting(String configKey)
        {
            String configValue;
            if (CONFIG.TryGetValue(configKey, out configValue))
            {
                return configValue;
            }else
            {
                return "";
            }
        }

        public static void SetConfigSetting(String configKey, String configValue)
        {

                CONFIG[configKey] = configValue;
           
        }

    }
}
