using System;
using UnityEngine;

namespace OpenGS
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class AutoCreateIfMissingAttribute : PropertyAttribute
    {
        public AutoCreateIfMissingAttribute(string childName)
            : this(childName, null, "Create")
        {
        }

        public AutoCreateIfMissingAttribute(string childName, Type componentType)
            : this(childName, componentType, "Create")
        {
        }

        public AutoCreateIfMissingAttribute(string childName, Type componentType, string buttonLabel)
        {
            ChildName = string.IsNullOrWhiteSpace(childName) ? string.Empty : childName;
            ComponentType = componentType;
            ButtonLabel = string.IsNullOrWhiteSpace(buttonLabel) ? "Create" : buttonLabel;
        }

        public string ChildName { get; }

        public Type ComponentType { get; }

        public string ButtonLabel { get; }
    }
}
