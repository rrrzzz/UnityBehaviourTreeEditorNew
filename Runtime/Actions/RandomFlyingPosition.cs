using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class RandomFlyingPosition : ActionNode
    {
        public NodeProperty<Vector3> destination;
        public float positionSpawnRadius = 20;

        public override void OnInit(){}

        protected override void OnStart(){}

        protected override void OnStop(){}

        protected override State OnUpdate()
        {
            if (TryGetRandomPositionInsideSphere(out var targetPos))
            {
                destination.Value = targetPos;
                return State.Success;
            }

            return State.Running;
        }
        
        private bool TryGetRandomPositionInsideSphere(out Vector3 randomPosition)
        {
            randomPosition = Random.insideUnitSphere * positionSpawnRadius;
            var posY = randomPosition.y;
            randomPosition = new Vector3(randomPosition.x, 0, randomPosition.z);
            randomPosition = context.transform.position + randomPosition;
            randomPosition.y = posY;

            var toGoal = context.transform.position - randomPosition;
            var distance = toGoal.magnitude;
            
            if (Physics.Raycast(context.transform.position, randomPosition, out var hit, distance))
            {
                var dirToGoal = toGoal.normalized;
                randomPosition = hit.point + Vector3.up * context.extents.y - dirToGoal * context.extents.z;
                return true;
            }
            
            return true;
        }
    }
}