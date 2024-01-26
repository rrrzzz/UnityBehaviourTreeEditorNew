using System.Collections;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class MoveToPositionNavmesh : MoveToPositionBase
    {
        public NodeProperty<Vector3> goalPosition = new NodeProperty<Vector3>(Vector3.zero);
        
        [Range(0, 10)] 
        public float rotationSpeed = 5;
        
        private const float DefaultAngularSpeed = 720;
        private const float RotationMultiplier = 200;
        
        private bool _isDestinationSet;

        public override void OnInit()
        {
            base.OnInit();
            if (!canRun)
            {
                return;
            }

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

            if (context.rb)
            {
                context.rb.isKinematic = true;
            }
            
            if (canJump)
            {
                context.agent.autoTraverseOffMeshLink = false;
            }
            
            context.agent.angularSpeed = DefaultAngularSpeed;
        }

        protected override void OnStart()
        {
            base.OnStart();

            if (!canRun)
            {
                return;
            }
            
            _isDestinationSet = false;
            
            context.agent.speed = speedScaled;
            context.agent.acceleration = accelerationScaled;
            context.agent.stoppingDistance = stoppingDistance;
        }

        protected override void OnStop(){}

        protected override State OnUpdate() 
        {
            if (!_isDestinationSet)
            {
                if (!context.agent.isOnNavMesh)
                {
                    return State.Running;
                }
                context.agent.destination = goalPosition;
                _isDestinationSet = true;
            }
            
            if (!isJumping)
            {
                UpdateMovementAnimation(context.agent.velocity.magnitude);
                HandleRotation();
            }
            
            if (canJump && !isJumping && context.agent.isOnOffMeshLink)
            {
                context.movementDataContainer.StartCoroutine(NavMeshParabolaJump());
            }

            if (isJumping)
            {
                return State.Running;
            }
            
            isGoalReached = CheckIfDestinationReached(); 
            if (isGoalReached)
            {
                UpdateMovementAnimation(0);
                return State.Success;
            }
  
            return State.Running;
        }

        public override void OnDrawGizmos() 
        {
            var agent = context.agent;

            // Current velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(context.transform.position, context.transform.position + agent.velocity);

            // Desired velocity
            Gizmos.color = Color.red;
            Gizmos.DrawLine(context.transform.position, context.transform.position + agent.desiredVelocity);

            // Current path
            Gizmos.color = Color.black;
            var agentPath = agent.path;
            Vector3 prevCorner = context.transform.position;
            foreach (var corner in agentPath.corners)
            {
                Gizmos.DrawLine(prevCorner, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                prevCorner = corner;
            }
        }
        
        private void HandleRotation()
        {
            if (context.agent.velocity == Vector3.zero)
            {
                return;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(context.agent.velocity);
            targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
                    
            // Smoothly rotate towards the target rotation
            context.transform.rotation = Quaternion.RotateTowards(context.transform.rotation, targetRotation, 
                rotationSpeed * RotationMultiplier * Time.deltaTime);
        }
        
        private bool CheckIfDestinationReached()
        {
            return !context.agent.pathPending && context.agent.remainingDistance <= context.agent.stoppingDistance &&
                   (!context.agent.hasPath || context.agent.velocity.sqrMagnitude == 0f);
        }
        
        private IEnumerator NavMeshParabolaJump()
        {
            context.agent.velocity = Vector3.zero;
            var endPos = context.agent.currentOffMeshLinkData.endPos;
            yield return context.movementDataContainer.StartCoroutine(ParabolaJump(endPos));
            context.agent.CompleteOffMeshLink();
        }
    }
}
