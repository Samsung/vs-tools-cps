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
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.Launcher
{
    public class ProfileLauncher
    {

        private ProfileSession _currentSession;

        private ProfileLauncher()
        {
        }

        public static ProfileLauncher Instance { get; private set; }

        public static void Initialize()
        {
            Instance = new ProfileLauncher();
        }

        public bool SessionActive => _currentSession != null;

        public IProfileSession CreateSession(ProfileSessionConfiguration sessionConfiguration)
        {
            return new ProfileSession(sessionConfiguration);
        }

        public void StartSession(IProfileSession session)
        {
            if (SessionActive)
            {
                throw new InvalidOperationException();
            }

            _currentSession = (ProfileSession)session;
            _currentSession.AddListener(new ProfileSessionListener(delegate(ProfileSessionState state)
            {
                if (state == ProfileSessionState.Finished || state == ProfileSessionState.Failed)
                {
                    _currentSession = null;
                }
            }));
            _currentSession.Start();

        }

        private class ProfileSessionListener : IProfileSessionListener
        {
            internal delegate void StateChangedEventHandler(ProfileSessionState newState);

            private readonly StateChangedEventHandler _handler;

            public ProfileSessionListener(StateChangedEventHandler handler)
            {
                _handler = handler;
            }

            public void StateChanged(ProfileSessionState newState)
            {
                _handler(newState);
            }

            public void SysInfoRead(SysInfoItem siItem)
            {
            }
        }

    }
}
