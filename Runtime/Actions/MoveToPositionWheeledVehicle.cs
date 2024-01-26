using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class MoveToPositionWheeledVehicle : ActionNode
    {
        public NodeProperty<Vector3> goalPosition = new NodeProperty<Vector3>(Vector3.zero);
        
        public float speed = 1;
        public float turnSpeed = 1;
        public bool brakeAtDestination = true;
        public float stoppingDistance = 1;
        
        private float brakingVariable = 1;
        private Vector3 directionToGoal;
        private float variableSpeed;
        private float distanceToGoal;
        private Vector3 direction;
        
        public override void OnInit()
        {
            if (!context.wheeledVehicleAnimator)
            {
                Debug.LogWarning("Missing VehicleAnimator, cannot execute MoveToPositionWheeledVehicle node.");
                canRun = false;
            }
        }

        protected override void OnStart(){}

        protected override void OnStop(){}

        protected override State OnUpdate()
        {
            if (!canRun)
            {
                return State.Failure;
            }
            
            variableSpeed = speed * brakingVariable;
            distanceToGoal = Vector3.Distance(goalPosition, context.transform.position);
            if (distanceToGoal < stoppingDistance)
            {
                return State.Success;
            }
            
            //Brake when close to target
            if (brakeAtDestination) 
            { 
                brakingVariable = Mathf.Clamp(distanceToGoal - brakingVariable, 0, 1); 
            }
            else
            {
                brakingVariable = 1; 
                
            }
            
            //Calculate vector to goal
            directionToGoal = new Vector3(goalPosition.Value.x, context.transform.position.y, goalPosition.Value.z) - 
                              context.transform.position;
            UpdateAnimationSpeed();
            
            //Blend animation
            MoveTowardsTarget();
            TurnTowardsTarget(directionToGoal);
            return State.Running;
        }
        
        public void TurnTowardsTarget(Vector3 directionToTarget)
        {
            // Turn towards the target
            var normalizedLookDirection = directionToTarget.normalized;
            var lookRotation = Quaternion.LookRotation(normalizedLookDirection);
            context.transform.rotation = Quaternion.Slerp(context.transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
            var direction = Vector3.Cross(normalizedLookDirection, context.transform.forward);
            context.wheeledVehicleAnimator.TurnToPercent(direction.y);
        }
        
        private void UpdateAnimationSpeed()
        {
            if (variableSpeed > 0.1)
            {
                context.wheeledVehicleAnimator.Accelerate();
            }
            else if (variableSpeed < -0.1)
            {
                context.wheeledVehicleAnimator.Decelerate();
            }
            else
            {
                context.wheeledVehicleAnimator.Decelerate();
            }
        }
        
        public void MoveTowardsTarget()
        {
            context.transform.position = Vector3.Lerp(context.transform.position, context.transform.position + 
                context.transform.forward, variableSpeed * Time.deltaTime);
        }
    }
}
