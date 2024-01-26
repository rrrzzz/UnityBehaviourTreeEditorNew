namespace AnythingWorld.Behaviour.Tree 
{
    [System.Serializable]
    public class SetProperty : ActionNode
    {
        public BlackboardKeyValuePair pair;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            pair.WriteValue();
            
            return State.Success;
        }
    }
}
