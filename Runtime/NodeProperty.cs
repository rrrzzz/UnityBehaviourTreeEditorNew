using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class NodeProperty
    {
        [SerializeReference]
        public BlackboardKey reference; 
    }

    [System.Serializable]
    public class NodeProperty<T> : NodeProperty
    {
        public T defaultValue;
        private BlackboardKey<T> _typedKey;

        public NodeProperty(){}

        public NodeProperty(T defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public static implicit operator T(NodeProperty<T> instance) => instance.Value;
        
        public T Value
        {
            set
            {
                if (typedKey != null)
                {
                    typedKey.value = value;
                }
                else
                {
                    defaultValue = value;
                }
            }
            get
            {
                if (typedKey != null)
                {
                    return typedKey.value;
                }
                else
                {
                    return defaultValue;
                }
            }
        }
        
        private BlackboardKey<T> typedKey
        {
            get
            {
                if (_typedKey == null && reference != null)
                {
                    _typedKey = reference as BlackboardKey<T>;
                }
                return _typedKey;
            }
        }

        public NodeProperty<T> CreateCopy()
        {
            var copy = new NodeProperty<T>
            {
                defaultValue = defaultValue,
                reference = reference
            };

            return copy;
        }
    }
}
