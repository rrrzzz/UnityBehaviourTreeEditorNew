using AnythingWorld.Animation;
using AnythingWorld.Animation.Vehicles;
using UnityEngine;
using UnityEngine.AI;

namespace AnythingWorld.Behaviour.Tree
{
    // The context is a shared object every node has access to.
    // Commonly used components and subsystems should be stored here
    // It will be somewhat specific to your game exactly what to add here.
    // Feel free to extend this class 
    public class Context
    {
        public GameObject gameObject;
        public Transform transform;
        public Rigidbody rb;
        public NavMeshAgent agent;
        public Vector3 extents;
        public MovementDataContainer movementDataContainer;
        
        public Animator animationController;
        public RunWalkIdleController legacyAnimationController;
        public VehicleAnimator wheeledVehicleAnimator;
        public FlyingVehicleAnimator flyingVehicleAnimator;
        
        // Add other game specific systems here

        public static Context CreateFromGameObject(GameObject gameObject) 
        {
            // Fetch all commonly used components
            Context context = new Context();
            context.gameObject = gameObject;
            context.transform = gameObject.transform;
            context.rb = gameObject.GetComponent<Rigidbody>();
            context.agent = gameObject.GetComponent<NavMeshAgent>();
            context.movementDataContainer = gameObject.GetComponent<MovementDataContainer>();
            context.legacyAnimationController = gameObject.GetComponentInChildren<RunWalkIdleController>();
            context.animationController = gameObject.GetComponentInChildren<Animator>();
            context.wheeledVehicleAnimator = gameObject.GetComponentInChildren<VehicleAnimator>();
            context.flyingVehicleAnimator = gameObject.GetComponentInChildren<FlyingVehicleAnimator>();
            if (context.movementDataContainer)
            {
                context.extents = context.movementDataContainer.extents;
            }
            
            return context;
        }
    }
}
