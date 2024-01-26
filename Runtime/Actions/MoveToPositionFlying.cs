using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class MoveToPositionFlying : ActionNode
    {
        public NodeProperty<Vector3> goalPosition = new NodeProperty<Vector3>();
        
        public float speed = 20;
        public float turnSpeed = 2;
        
        public bool brakeAtDestination = true;
        
        public float goalRadius = 10;
        
        private float currentBreaking = 1;
        private Vector3 directionToGoal;
        private float currentSpeed;
        private float distanceToGoal;
        
        private Vector3 direction;
        private Vector3 currentGoalPosition;
        
        public override void OnInit()
        {
            if (!context.flyingVehicleAnimator)
            {
                Debug.LogWarning("Missing flyingVehicleAnimator, cannot execute MoveToPositionFlying node.");
                canRun = false;
            }
        }

        protected override void OnStart()
        {
            currentGoalPosition = goalPosition.Value;
        }

        protected override void OnStop(){}

        protected override State OnUpdate()
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            currentSpeed = speed * currentBreaking;
            currentGoalPosition = new Vector3(currentGoalPosition.x, context.transform.position.y, currentGoalPosition.z);
            distanceToGoal = Vector3.Distance(goalPosition, context.transform.position);
            
            if (distanceToGoal < goalRadius)
            {
                return State.Success;
            }
            
            //Brake when close to target
            if (brakeAtDestination)
            {
                currentBreaking = Mathf.Clamp(distanceToGoal - currentBreaking, 0, 1); 
            }
            else
            {
                currentBreaking = 1; 
            }
            
            //Calculate vector to goal
            directionToGoal = goalPosition - context.transform.position;

            if (currentSpeed > 0.1)
            {
                context.flyingVehicleAnimator.Accelerate();
            }
            else
            {
                context.flyingVehicleAnimator.Deceleration();
            }
            
            //Blend animation
            MoveTowardsTarget();
            TurnTowardsTarget(directionToGoal);
            return State.Running;
        }
        
        public void MoveTowardsTarget()
        {
            context.transform.position = Vector3.Lerp(context.transform.position, context.transform.position + 
                context.transform.forward, currentSpeed * Time.deltaTime);
        }

        public void TurnTowardsTarget(Vector3 directionToTarget)
        {
            var normalizedLookDirection = directionToTarget.normalized;

            var lookRotation = Quaternion.LookRotation(normalizedLookDirection);
            context.transform.rotation = Quaternion.Slerp(context.transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }
}
