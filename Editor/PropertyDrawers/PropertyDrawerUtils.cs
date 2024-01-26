using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AnythingWorld.Behaviour.Tree
{
    public static class PropertyDrawerUtils
    {
        public static VisualElement GetFieldByType(Type propertyType, string bindingPath)
        {
            if (propertyType == typeof(Vector4))
            {
                return new Vector4Field
                {
                    bindingPath = bindingPath
                };
            }
            if (propertyType == typeof(Bounds))
            {
                return new BoundsField()
                {
                    bindingPath = bindingPath
                };
            }
            if (propertyType == typeof(BoundsInt))
            {
                return new BoundsIntField()
                {
                    bindingPath = bindingPath
                };
            }

            var defaultValueField = new PropertyField
            {
                bindingPath = bindingPath
            };

            return defaultValueField;
        }
    }
}
