using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    public abstract class DecoratorNode : Node
    {
        [SerializeReference]
        [HideInInspector] 
        public Node child;
    }
}
