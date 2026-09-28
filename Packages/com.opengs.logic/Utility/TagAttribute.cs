using OpenGSCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenGSCore
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class TagAttribute : Attribute
    {
        public string Label { get; }
        public TagAttribute(string label) => Label = label;
    }
}
