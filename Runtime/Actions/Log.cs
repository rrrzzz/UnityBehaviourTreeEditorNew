using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class Log : ActionNode
    {
        [Tooltip("Message to log to the console")]
        public NodeProperty<string> message = new NodeProperty<string>();
        public NodeProperty<Vector4> sss = new NodeProperty<Vector4>();
        public NodeProperty<GameObject> aaa = new NodeProperty<GameObject>();
        public NodeProperty<Rigidbody> rb = new NodeProperty<Rigidbody>();
        public NodeProperty<Collider> rcccb = new NodeProperty<Collider>();
        public NodeProperty<Bounds> xxzx = new NodeProperty<Bounds>();
        public NodeProperty<BoundsInt> xxzzxczx = new NodeProperty<BoundsInt>();

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            Debug.Log($"{message.Value}");
            return State.Success;
        }
    }
}
