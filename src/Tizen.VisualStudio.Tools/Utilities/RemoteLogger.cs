/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using Tizen.VisualStudio.Tools.Data;
using System.Diagnostics;
using Tizen.VisualStudio.Tools.Utilities;
using System.Windows.Forms;

namespace Tizen.VisualStudio.Utilities
{
    public class AnalyticConfig
    {
        public String id;
        public Boolean logging;

        public AnalyticConfig(String uid, Boolean log)
        {
            id = uid;
            logging = log;
        }
    }

    public class ActionLog
    {
        public String id;
        public String type;
        public String product;
        public long usage;
        public String version;
        public String timestamp;

        public ActionLog()
        {
        }

        public ActionLog(String id, String type, String product, String version, String timestamp)
        {
            this.id = id;
            this.type = type;
            this.product = product;
            this.version = version;
            this.timestamp = timestamp;
        }
    }

    public class UsageLog : ActionLog
    {
        public UsageLog(String id, String type, String product, String version, String timestamp, long usage) :
            base(id, type, product, version, timestamp)
        {
            base.usage = usage;
        }
    }

    public class DeleteUser : ActionLog
    {
        public DeleteUser(String id)
        {
            base.id = id;
        }
    }

    public class AccessLog : ActionLog
    {
        public AccessLog(String id, String type, String product, String version, String timestamp) :
            base(id, type, product, version, timestamp)
        {

        }
    }
    public class RemoteLogger
    {
        private const string DEFAULT_VERSION = "Default";
        private const string DIR_IDE = "ide";
        private const string ANALYTICS_CONFIG_FILE = "analytics.conf";
        private const string LOGGING_URL = "https://1lxb5yo2lb.execute-api.ap-northeast-2.amazonaws.com/v1";
        private static Dictionary<String, String> mapUrl = getMapValues();
        private static AnalyticConfig ParseConfig(string filePath)
        {
            AnalyticConfig config = new AnalyticConfig("", false);
            try
            {
                if(filePath!=null && File.Exists(filePath))
                {
                    AnalyticConfig data = JsonConvert.DeserializeObject<AnalyticConfig>(File.ReadAllText(filePath));
                    if (data != null && data.id != null && (data.logging == true || data.logging == false)) {
                        config.id = data.id;
                        config.logging = data.logging;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Couldn't find file while parsing\n");
                return config;
            }
            return config;
        }
        private static Dictionary<String, String> getMapValues()
        {
            Dictionary<String, String> map = new Dictionary<String, String>();
            map.Add("usage", "postusage");
            map.Add("access", "postaccess");
            map.Add("deleteUser", "deleteuser");
            return map;
        }

        public static Boolean isLoggingEnabled()
        {
            return getAnalyticsConf().logging;
        }

        public static void writeLoggingInfoToFile(Boolean log)
        {
            writeAnalyticsConf(getAnalyticsConf().id, log);
        }

        public static String getIdeUserDataPath()
        {
            string UserDataPath = ToolsPathInfo.UserDataFolderPath;
            if (String.IsNullOrEmpty(UserDataPath))
            {
                Console.WriteLine("SDKPath Path is null\n");
                return null;
            }
            return Path.Combine(UserDataPath, DIR_IDE);
        }

        private static String getAnalyticsConfigPath()
        {
            String dataPath = getIdeUserDataPath();
            if (dataPath != null)
            {
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
            }
            else
            {
                Console.WriteLine("Couldn't create file to get Analytics config.\n");
                return null;
            }
            string filePath = Path.Combine(dataPath , ANALYTICS_CONFIG_FILE);
            return filePath.ToString();
        }

        private static AnalyticConfig writeAnalyticsConf(String uuid, Boolean log)
        {
            AnalyticConfig config = new AnalyticConfig(uuid, log);
            string filePath = getAnalyticsConfigPath();
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Config file path is null.\n");
                return config;
            }
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, JsonConvert.SerializeObject(config));
            return config;
        }

        private static AnalyticConfig getAnalyticsConf()
        {
            string filePath = getAnalyticsConfigPath();
            AnalyticConfig config = ParseConfig(filePath);
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Config file path is null.\n");
                return config;
            }
            if (String.IsNullOrEmpty(config.id))
            {
                File.Delete(filePath);
                Guid guid = Guid.NewGuid();
                string str = guid.ToString();
                String id = str.Replace("-", "");
                return writeAnalyticsConf(id, true);
            }
            return config;
        }

        private static String getVersion() {
            string filePath = Path.Combine(ToolsPathInfo.ToolsRootPath,"sdk.version");
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("Version file path is null.\n");
                return null;
            }
            byte[] encoded = File.ReadAllBytes(filePath.ToString());
            String content = System.Text.Encoding.UTF8.GetString(encoded);
            String[] data = content.Split('=');
            if (data.Length < 2)
                return DEFAULT_VERSION;
            data[1] = data[1].Replace("\n", "");
            data[1] = data[1].Replace("\t", "");
            data[1] = data[1].Replace(" ", "");
            
            return data[1];

        }

        private static void sendLogs(ActionLog actlog, String type)
        {
            string ResponseString;
            Uri url;
            if (!Uri.TryCreate((LOGGING_URL + "/" + mapUrl[type]), UriKind.Absolute, out url))
            {
                Console.WriteLine("Error while making valid url from sting.\n");
            }
            else
            {
                try
                {
                    var postData = JsonConvert.SerializeObject(actlog);
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(postData);
                    WebRequest request;
                    request = WebRequest.Create(url);
                    request.Method = "POST";
                    request.Timeout = 5000;
                    request.ContentType = "application/json; charset=UTF-8";
                    request.ContentLength = byteArray.Length;


                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse response = (WebResponse)request.GetResponse();
                    ResponseString = "Status : " + response.ToString();
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        WebResponse response = (HttpWebResponse)ex.Response;
                        ResponseString = "Protocol Error occured: " + response.ToString();
                    }
                    else
                    {
                        ResponseString = "Error occured: " + ex.Status.ToString();
                    }
                    Console.WriteLine(ResponseString);
                    return;
                }
            }
        }

        private static void createLog(String product, long usage, String type)
        {
            AnalyticConfig conf = getAnalyticsConf();
            String uid = conf.id;
            if (String.IsNullOrEmpty(uid))
            {
                Console.WriteLine("Failed to generate UID.\n");
                return;
            }
            if (!AnalyticsInfo.UseAnalytics)
            {
                Console.WriteLine("Logging is disabled by user.\n");
                if (type != "deleteUser")
                    return;
            }

            DateTimeOffset time = DateTimeOffset.UtcNow;
            string format = "yyyy-MM-dd'T'HH:mm:ss'Z'";
            String timestamp = time.ToString(format);
            String version;
            try
            {
                version = getVersion();
            }
            catch
            {
                Console.WriteLine("Failed to get version.\n");
                version = DEFAULT_VERSION;
            }
            ActionLog actlog;
            if (type.Equals("access"))
            {
                actlog = new AccessLog(uid, type, product, version, timestamp);
            }
            else if (type.Equals("usage"))
            {
                actlog = new UsageLog(uid, type, product, version, timestamp, usage);
            }
            else
            {
                actlog = new DeleteUser(uid);
            }
            try
            {
                sendLogs(actlog, type);
            }
            catch
            {
                Console.WriteLine("Post Request failed while trying to access server URL.\n");
            }
        }

        public static void logAccess(String product)
        {
            createLog(product, 0, "access");
        }

        public static void deleteAnalytics()
        {
            createLog(null, 0, "deleteUser");
        }

        public static void logUsage(String product, long usage)
        {
            createLog(product, usage, "usage");
        }
    }
}

