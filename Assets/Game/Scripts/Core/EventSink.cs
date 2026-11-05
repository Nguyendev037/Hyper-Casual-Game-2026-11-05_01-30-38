namespace RHMG.Core
{
    /// <summary>
    /// Event names: run_start, floor_clear, player_death, spirit_rescue, fusion_preview,
    /// fusion_confirm, outfit_purchase, showcase_open, run_complete.
    /// </summary>
    public interface IEventSink
    {
        void Log(string eventName, string payload);
    }

    public sealed class NullEventSink : IEventSink
    {
        public void Log(string eventName, string payload)
        {
        }
    }
}
