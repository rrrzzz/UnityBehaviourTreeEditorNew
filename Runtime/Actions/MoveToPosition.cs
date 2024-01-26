using System.Collections;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public class MoveToPosition : MoveToPositionBase 
    {
        public NodeProperty<Vector3> goalPosition = new NodeProperty<Vector3>(Vector3.zero);
        
        public float turnAcceleration = 90;
        public float angularSpeed = 360.0f;
        
        public float maxJumpHeight = 3f;
        public float jumpDetectorSize = 3;
        public float jumpDetectorSizeScaled;
        public float maxSlope = 45;
        public bool scaleJumpDetectorWithHeight = true;
        
        private const float PositionDelta = .001f;
        private const float StepHeightMultiplier = 0.7f;
        
        private float _distanceToGoal;
        private Vector3 _velocity;
        private float _stepHeight;
       
        public override void OnInit()
        {
            base.OnInit();
            if (!canRun)
            {
                return;
            }

            if (context.rb)
            {
                context.rb.freezeRotation = true;
            }
            else
            {
                Debug.LogWarning(@"Cannot run ""MoveToPosition"" node without rigidbody.");
                canRun = false;
                return;
            }

            PlaceOnGround();
            
            _stepHeight = context.extents.y * StepHeightMultiplier;
        }
        
        protected override void OnStart()
        {
            base.OnStart();
            
            if (!canRun)
            {
                return;
            }
            
            jumpDetectorSizeScaled = scaleJumpDetectorWithHeight ? jumpDetectorSize * context.extents.y : jumpDetectorSize;
            
            context.rb.isKinematic = false;
            context.rb.useGravity = true;

            context.movementDataContainer.StartCoroutine(FixedUpdateCoroutine());
        }

        protected override void OnStop()
        {
            context.movementDataContainer.StopCoroutine(FixedUpdateCoroutine());
        }

        protected override State OnUpdate() 
        {
            if (!canRun)
            {
                return State.Failure;
            }

            if (isGoalReached)
            {
                return State.Success;
            }

            return State.Running;
        }

        public override void OnDrawGizmos() 
        {
            var transform = context.transform;

            // Current velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + _velocity);

            // Current path
            Gizmos.color = Color.black;
            Gizmos.DrawLine(transform.position, goalPosition);
        }
        
        private IEnumerator FixedUpdateCoroutine()
        {
            while (!isGoalReached)
            {
                _distanceToGoal = Vector3.Distance(goalPosition, context.transform.position);
            
                if (_distanceToGoal <= stoppingDistance)
                {
                    UpdateMovementAnimation(0);
                    context.rb.isKinematic = true;
                    isGoalReached = true;
                    break;
                }
         
                if (canJump && !isJumping)
                {
                    JumpIfNecessary();
                }

                if (isGoalReached)
                {
                    break;
                }
                if (!isJumping)
                {
                    MoveTowardsTarget();
                    UpdateMovementAnimation(_velocity.magnitude);
                }
                
                yield return new WaitForFixedUpdate();
            }
        } 
        
        private void MoveTowardsTarget()
        {
            context.rb.useGravity = true;
            context.rb.isKinematic = false;
            
            if (Physics.Raycast(context.transform.position, Vector3.down, out var hit, context.extents.y + 0.1f))
            {
                // Check if the surface is a slope
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

                var forwardToGoal = goalPosition - context.transform.position;
                forwardToGoal.y = context.transform.position.y;
                forwardToGoal.Normalize();
                
                float dir = Vector3.Dot(forwardToGoal, hit.normal);

                var isDownwardSlope = dir > 0;
                
                if (!isDownwardSlope)
                {
                    if (slopeAngle > 0 && slopeAngle < maxSlope)
                    {
                        context.rb.useGravity = false;
                    }
                    else if (slopeAngle > maxSlope)
                    {
                        isGoalReached = true;
                        return;
                    }
                }
            }

            var heightDif = goalPosition.Value.y - context.transform.position.y;
            Vector3 direction;
            if (heightDif > _stepHeight || heightDif < 0)
            {
                var goalLeveled = goalPosition.Value;
                goalLeveled.y = context.transform.position.y;
                direction = (goalLeveled - context.transform.position).normalized;
            }
            else
            {
                direction = (goalPosition - context.transform.position).normalized;
            }
            
            Vector3 targetVelocity = direction * speedScaled;
           
            // Determine the angle between current velocity and target direction
            float turnAngle = Vector3.Angle(_velocity, targetVelocity);
            // Scale acceleration based on turn angle
            float finalAcceleration = Mathf.Lerp(accelerationScaled, turnAcceleration, turnAngle / 180f);
            // Apply acceleration
            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, finalAcceleration * Time.deltaTime);

            // Calculate rotation towards target
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                context.transform.rotation = Quaternion.RotateTowards(context.transform.rotation, targetRotation, angularSpeed * Time.deltaTime);
            }

            // Apply velocity
            context.rb.MovePosition(context.transform.position + _velocity * Time.deltaTime);
        }
        
        private void JumpIfNecessary()
        {
            var toGoalVector = goalPosition - context.transform.position;
            if (Vector3.Dot(toGoalVector, context.transform.forward) < 0.2f)
            {
                return;
            }
            
            if (!TryFindObstacleAhead(out var obstacleHitPoint))
            {
                return;
            }
            
            var distToGoal = Vector3.Distance(goalPosition, context.transform.position);
            var distToObstacle = Vector3.Distance(context.transform.position, new Vector3(obstacleHitPoint.x,
                context.transform.position.y, obstacleHitPoint.z));

            if (distToGoal < distToObstacle)
            {
                return;
            }

            if (!TryFindSurfacePoint(MaxRaycastDistance / 2, obstacleHitPoint, out var obstacleSurfacePoint))
            {
                return;
            }
            
            var obstacleHeightDifference = GetObstacleSurfaceHeightDifference(obstacleSurfacePoint.y);

            var approximateLegHeight = context.extents.y * .6f;
            if (obstacleHeightDifference <= approximateLegHeight)
            {
                return;
            }

            if (obstacleHeightDifference > maxJumpHeight)
            {
                isGoalReached = true;
                return;
            }

            context.rb.isKinematic = true;

            _velocity = Vector3.zero;

            var landingPoint = obstacleSurfacePoint + Vector3.up * context.extents.y;

            var isObstacleHeightEqualGoal = Mathf.Approximately(goalPosition.Value.y, landingPoint.y);
            var distanceToObstacleSurface = Vector3.Distance(context.transform.position, landingPoint);

            if (isObstacleHeightEqualGoal && _distanceToGoal < distanceToObstacleSurface)
            {
                landingPoint = goalPosition;
            }

            context.movementDataContainer.StartCoroutine(ParabolaJump(landingPoint));
        }
        
        private float GetObstacleSurfaceHeightDifference(float obstacleY) => obstacleY - context.transform.position.y + context.extents.y;

        private bool TryFindObstacleAhead(out Vector3 obstacleHitPoint)
        {
            obstacleHitPoint = Vector3.zero;

            var goal = goalPosition.Value;
            goal.y = context.transform.position.y;

            var dirToGoal = goal - context.transform.position;
            dirToGoal.Normalize();
            
            var originToGoal = context.transform.position - Vector3.up * PositionDelta + dirToGoal * context.extents.z;
            
            if (Physics.Raycast(originToGoal, dirToGoal, out var hitToGoal, jumpDetectorSizeScaled))
            {
                float slopeAngle = Vector3.Angle(hitToGoal.normal, Vector3.up);
                if (slopeAngle < maxSlope)
                {
                    return false;
                }
    
                obstacleHitPoint = hitToGoal.point;
                return true;
            }
            
            var origin = context.transform.position - Vector3.up * PositionDelta + context.transform.forward * context.extents.z;
            
            if (Physics.Raycast(origin, context.transform.forward, out var hit, jumpDetectorSizeScaled))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle < maxSlope)
                {
                    return false;
                }
    
                obstacleHitPoint = hit.point;
                return true;
            }
   
            return false;
        }
        
        private void PlaceOnGround()
        {
            if (Physics.Raycast(context.transform.position, Vector3.down, out var s, MaxRaycastDistance))
            {
                context.transform.position = s.point + Vector3.up * context.extents.y;
            }
        }
        
        private bool TryFindSurfacePoint(float upDistance, Vector3 initialPoint, out Vector3 surfacePoint)
        {
            var origin = initialPoint + Vector3.up * upDistance;
            if (Physics.Raycast(origin, Vector3.down, out var hit, MaxRaycastDistance))
            {
                surfacePoint = hit.point;
                return true;
            }

            surfacePoint = Vector3.zero;
            return false;
        }
    }
}
