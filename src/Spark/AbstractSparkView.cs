﻿/*
   Copyright 2008 Louis DeJardin - http://whereslou.com

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
using System.IO;
using System.Text;

namespace Spark
{
    public abstract class AbstractSparkView : ISparkView
    {
        private readonly Dictionary<string, TextWriter> _content = new Dictionary<string, TextWriter>();

        public Dictionary<string, TextWriter> Content { get { return _content; } }
        public TextWriter Output { get; set; }


        public IDisposable OutputScope(string name)
        {
            TextWriter writer;
            if (!_content.TryGetValue(name, out writer))
            {
                writer = new StringWriter();
                _content.Add(name, writer);
            }
            return new OutputScopeImpl(this, writer);
        }

        public IDisposable OutputScope(TextWriter writer)
        {
            return new OutputScopeImpl(this, writer);
        }

        class OutputScopeImpl : IDisposable
        {
            private readonly AbstractSparkView view;
            private readonly TextWriter previous;


            public OutputScopeImpl(AbstractSparkView view, TextWriter writer)
            {
                this.view = view;
                previous = view.Output;
                view.Output = writer;
            }

            public void Dispose()
            {
                view.Output = previous;
            }
        }


        public abstract void RenderView(TextWriter writer);
    }
}