using UnityEngine;
using UnityEngine.AI;

namespace AnythingWorld.Behaviour.Tree 
{
    [System.Serializable]
    public class RandomPositionNavMesh : ActionNode 
    {
        public NodeProperty<Vector3> destination;
        public float maxGoalDistance = 10f;
        
        private Vector3 _randomDir;
        private float _navMeshSampleDistance;
        
        public override void OnInit()
        {
            canRun = false;
            if (!context.agent)
            {
                Debug.LogWarning($"Game object {context.gameObject.name} is missing NavMeshAgent component, cannot continue.");
                return;
            }
                        
            if (!context.agent.isActiveAndEnabled)
            {
                Debug.LogWarning($"{context.gameObject.name} NavMeshAgent is not enabled. Cannot generate NavMesh path");
                return;
            }

            canRun = true;
        }

        protected override void OnStart() 
        {
            if (!canRun)
            {
                return;
            }
            
            // As recommended in docs https://docs.unity3d.com/ScriptReference/AI.NavMesh.SamplePosition.html
            _navMeshSampleDistance = context.agent.height * 2;
  
            _navMeshSampleDistance = context.extents.y / context.transform.lossyScale.y * 4;
        }

        protected override void OnStop() 
        {
        }

        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            if (TryGenerateNewMovementGoal())
            {
                return State.Success;
            }
         
            return State.Running;
        }
        
        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(_randomDir, 1);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(destination.Value, 1);
        }
        
        private bool TryGenerateNewMovementGoal()
        {
            _randomDir = context.transform.position + Random.insideUnitSphere * maxGoalDistance;
                
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = context.agent.agentTypeID,
                areaMask = NavMesh.AllAreas
            };

            if (!NavMesh.SamplePosition(_randomDir, out var hit, _navMeshSampleDistance, filter))
            {
                return false;
            }
            
            destination.Value = hit.position;
            return true;
        }
    }
}
