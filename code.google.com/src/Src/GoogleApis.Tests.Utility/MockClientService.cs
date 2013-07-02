﻿/*
Copyright 2013 Google Inc

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;

using Google.Apis.Discovery;
using Google.Apis.Services;

namespace Google.Apis.Testing
{
    /// <summary>
    /// Mock-Response for testing purposes.
    /// </summary>
    public class MockClientService : BaseClientService
    {
        public override string Name { get { return "TestService"; } }

        private string _baseUri;
        public override string BaseUri { get { return _baseUri; } }

        private IList<string> _features = new List<string> { "rest", "rpc", "json", "atom" };
        public override IList<string> Features { get { return _features; } }
        public void SetFeatures(IList<string> features) { _features = features; }

        public override IDictionary<string, IParameter> ServiceParameters { get { return null; } }

        public override Google.Apis.Requests.IRequest CreateRequest(Google.Apis.Requests.IClientServiceRequest request)
        {
            throw new NotImplementedException();
        }

        public MockClientService()
            : this(new Initializer(), @"https://testexample.google.com")
        {
        }
        public MockClientService(Initializer initializer)
            : this(initializer, @"https://testexample.google.com")
        {
        }
        public MockClientService(string baseUri)
            : this(new Initializer(), baseUri)
        {
        }
        public MockClientService(Initializer initializer, string baseUri)
            : base(initializer)
        {
            _baseUri = baseUri;
        }

    }
}
