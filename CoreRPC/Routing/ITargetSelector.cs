namespace CoreRPC.Routing
{
    public interface ITargetSelector
    {
        object GetTarget(string target);
    }
}
