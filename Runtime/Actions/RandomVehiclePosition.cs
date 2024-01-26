using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class RandomVehiclePosition : ActionNode
    {
        public NodeProperty<Vector3> destination;
        public float positionSpawnRadius = 20;
        
        protected override void OnStart()
        {}

        protected override void OnStop()
        {}

        protected override State OnUpdate()
        {
            destination.Value = GetRandomPositionInsideSphere();
            return State.Success;
        }
        
        private Vector3 GetRandomPositionInsideSphere()
        {
            var randomPosition = Random.insideUnitSphere * positionSpawnRadius;
            randomPosition = new Vector3(randomPosition.x, 0, randomPosition.z);
            randomPosition = context.transform.position + randomPosition;
            return randomPosition;
        }
    }
}


