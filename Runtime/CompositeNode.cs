using System.Collections.Generic;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public abstract class CompositeNode : Node
    {
        [HideInInspector] 
        [SerializeReference]
        public List<Node> children = new List<Node>();
    }
}
