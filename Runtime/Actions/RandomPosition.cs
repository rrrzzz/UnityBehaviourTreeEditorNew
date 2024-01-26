using UnityEngine;

namespace AnythingWorld.Behaviour.Tree 
{
    [System.Serializable]
    public class RandomPosition : ActionNode 
    {
        public NodeProperty<Vector3> destination;
        public float minGoalDistance = 2;
        public float maxGoalDistance = 10;
        
        private Vector3 _randomDir;

        protected override void OnStart() 
        {
            if (context.extents == Vector3.zero)
            {
                Debug.LogWarning("Imported model doesn't have capsule collider, cannot get model dimensions and start " +
                                 "random movement.");
                canRun = false;
                return;
            }
            
            canRun = true;
        }

        protected override void OnStop() {}

        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }

            if (TryGetRandomPositionInsideSphere())
            {
                return State.Success;
            }

            return State.Running;
        }
        
        private bool TryGetRandomPositionInsideSphere()
        {
            var randomPosition = Random.insideUnitSphere.normalized * Random.Range(minGoalDistance, maxGoalDistance);
            randomPosition = new Vector3(randomPosition.x, 0, randomPosition.z);
            randomPosition = context.transform.position + randomPosition;

            if (!Physics.Raycast(randomPosition + Vector3.up * MoveToPositionBase.MaxRaycastDistance / 2, Vector3.down, out var hit,
                    MoveToPositionBase.MaxRaycastDistance))
            {
                return false;
            }
            
            destination.Value = hit.point + Vector3.up * context.extents.y;
            return true;
        }
        
        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_randomDir, 1);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(destination.Value, 1);
        }
    }
}
