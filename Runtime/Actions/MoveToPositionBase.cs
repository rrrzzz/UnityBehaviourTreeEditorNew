using System.Collections;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace AnythingWorld.Behaviour.Tree
{
    [System.Serializable]
    public abstract class MoveToPositionBase : ActionNode
    {
        public const int MaxRaycastDistance = 100;
        
        public bool canJump = true;
        public float speed = 2;
        public float acceleration = 4;
        public bool scaleSpeedWithModelSpeed = true;
        public float stoppingDistance = 0.1f;
        
        protected bool isJumping;
        protected bool isGoalReached;
        protected float speedScaled  => scaleSpeedWithModelSpeed ? speed * _speedScalar : speed;
        protected float accelerationScaled  => scaleSpeedWithModelSpeed ? acceleration * _speedScalar : speed; 
        
        private const float JumpCurveHeight = 2;
        private const string JumpStartName = "jump_start";
        private const string JumpFallName = "jump_fall";
        private const string JumpEndName = "jump_end";
        private const string BlendTreeStateName = "Blend Tree";
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int FallingId = Animator.StringToHash("Falling");
        private static readonly int AnimationSpeedIdx = Animator.StringToHash("Speed");
        
        private bool _hasJumpAnimation;
        private bool _isFallingLocked;
        private float _jumpEndDuration;
        private bool _isLanding;
        private float _jumpAnimationDuration;
        private float _speedScalar;
        
        public override void OnInit()
        {
            if (context.extents == Vector3.zero)
            {
                Debug.LogWarning("Imported model doesn't have capsule collider, cannot get model dimensions and start " +
                                 "random movement.");
                canRun = false;
                return;
            }

            _speedScalar = context.movementDataContainer.speedScalar;
            
            if (context.legacyAnimationController)
            {
                _hasJumpAnimation = context.legacyAnimationController.loadedAnimationNames.Contains(JumpFallName);
            }
            else
            {
                _hasJumpAnimation = context.animationController.runtimeAnimatorController.animationClips.
                    Any(x => x.name.StartsWith("jump"));
            }
            
            if (_hasJumpAnimation)
            {
                GetJumpAnimationDurations();
            }
        }

        protected override void OnStart()
        {
            isGoalReached = false;
        }
        
        protected void UpdateMovementAnimation(float speed)
        {
            if (context.legacyAnimationController)
            {
                context.legacyAnimationController.BlendAnimationOnSpeed(speed);
            }
            else
            {
                context.animationController.SetFloat(AnimationSpeedIdx, speed);
            }
        }
        
        protected IEnumerator ParabolaJump(Vector3 endPosition)
        {   
            isJumping = true;
            _isFallingLocked = _isLanding = false;
            
            Vector3 startPosition = context.transform.position;
            
            var heightDifference = endPosition.y - startPosition.y;

            var finalHeight = Mathf.Max(.5f, heightDifference);
            var jumpDuration = RemapHeightToJumpDuration(finalHeight);
            if (heightDifference < 0)
            {
                jumpDuration *= .9f;
            }

            if (_hasJumpAnimation)
            {
                var animationToJumpDurationRatio = _jumpAnimationDuration / jumpDuration;
                var isJumpFasterThanAnimation = animationToJumpDurationRatio > 1;
                if (isJumpFasterThanAnimation)
                {
                    SetJumpAnimationSpeed(animationToJumpDurationRatio);
                }
            }

            if (_hasJumpAnimation)
            {
                PlayJumpAnimation();
            }
            
            RotateToJumpTarget(endPosition, startPosition);

            float normalizedTime = 0.0f;
            float elapsedTime = 0;
            while (normalizedTime < 1.0f)
            {
                float yOffset = finalHeight * JumpCurveHeight * (normalizedTime - normalizedTime * normalizedTime);
                context.transform.position = Vector3.Lerp(startPosition, endPosition, normalizedTime) + yOffset * Vector3.up;
                normalizedTime += Time.deltaTime / jumpDuration;
                elapsedTime += Time.deltaTime;
                if (_hasJumpAnimation)
                {
                    ProcessJumpAnimations(elapsedTime, jumpDuration);
                }
                
                yield return null;
            }

            if (_hasJumpAnimation)
            {
                SetJumpAnimationSpeed(1);
            }

            isJumping = false;
        }
        
        private void GetJumpAnimationDurations()
        {
            if (context.legacyAnimationController)
            {
                _jumpAnimationDuration = context.legacyAnimationController.loadedAnimationDurations.Sum();
                _jumpEndDuration = context.legacyAnimationController.animationNamesToDurations[JumpEndName];
                return;
            }
            
            var overrideController = context.animationController.runtimeAnimatorController as AnimatorOverrideController;
            var controller = overrideController.runtimeAnimatorController as AnimatorController;

            foreach (var childState in controller.layers[0].stateMachine.states)
            {
                if (childState.state.name != BlendTreeStateName)
                {
                    _jumpAnimationDuration += childState.state.transitions[0].duration;
                }

                if (childState.state.name == JumpEndName)
                {
                    _jumpEndDuration = childState.state.transitions[0].duration;
                }
            }
        }
        
        private void SetJumpAnimationSpeed(float speed)
        {
            if (context.legacyAnimationController)
            {
                context.legacyAnimationController.currentAnimationSpeed = speed;
                context.legacyAnimationController.animationPlayer[JumpStartName].speed = speed;
                context.legacyAnimationController.animationPlayer[JumpFallName].speed = speed;
                context.legacyAnimationController.animationPlayer[JumpEndName].speed = speed;
                return;
            }
                
            context.animationController.speed = speed;
        }

        private float GetJumpAnimationSpeed()
        {
            if (context.legacyAnimationController)
            {
                return context.legacyAnimationController.animationPlayer[JumpFallName].speed;
            }
                
            return context.animationController.speed;
        }
        
        private float RemapHeightToJumpDuration(float value)
        {
            var heightLowerBound = 0.76f;
            var heightUpperBound = 2.96f;

            var durationLowerBound = 0.5f;
            var durationUpperBound = 0.8f;
            
            return (value - heightLowerBound) / (heightUpperBound - heightLowerBound) * 
                (durationUpperBound - durationLowerBound) + durationLowerBound;
        }

        private void RotateToJumpTarget(Vector3 endPos, Vector3 startPos)
        {
            var dirToEnd = endPos - startPos;
            dirToEnd.y = context.transform.position.y;
            context.transform.rotation = Quaternion.LookRotation(dirToEnd);
        }

        private void ProcessJumpAnimations(float elapsedTime, float jumpDuration)
        {
            if (!context.legacyAnimationController && !(GetJumpAnimationSpeed() <= 1)) return;
            
            LockAnimationIfStartedFalling();
            PlayJumpEndAnimation(elapsedTime, jumpDuration);
        }
        
        private void PlayJumpAnimation()
        {
            if (context.legacyAnimationController)
            {
                context.legacyAnimationController.JumpStart();
            }
            else
            {
                context.animationController.SetTrigger(JumpId);
            }
        }

        private void PlayJumpEndAnimation(float elapsedTime, float jumpDuration)
        {
            var jumpEndDurationScaled = _jumpEndDuration;
            if (context.legacyAnimationController)
            {
                jumpEndDurationScaled /= GetJumpAnimationSpeed();
            }

            if (_isLanding || jumpDuration - elapsedTime > jumpEndDurationScaled)
            {
                return;
            }
            
            _isLanding = true;
            
            if (context.legacyAnimationController)
            {
                context.legacyAnimationController.JumpEnd();
            }
            else
            {
                context.animationController.SetBool(FallingId, false);
            }
        }
        
        private void LockAnimationIfStartedFalling()
        {
            if (!context.animationController)
            {
                return;
            }
            
            if (!_isFallingLocked && context.animationController.GetCurrentAnimatorStateInfo(0).IsName(JumpFallName))
            {
                _isFallingLocked = true;
                context.animationController.SetBool(FallingId, true);
            }
        }
    }
}

