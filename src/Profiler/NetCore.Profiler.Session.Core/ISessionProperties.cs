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

namespace NetCore.Profiler.Session.Core
{
    public interface ISessionProperties
    {
        string GetProperty(string mainKey, string key);

        bool GetBoolProperty(string mainKey, string key, bool defaultValue);

        int GetIntProperty(string mainKey, string key, int defaultValue);

        void SetProperty(string mainKey, string key, string value);

        void SetProperty(string mainKey, string key, bool value);

        void SetProperty(string mainKey, string key, int value);

        bool PropertyExists(string mainKey);

        bool PropertyExists(string mainKey, string key);
    }
}
