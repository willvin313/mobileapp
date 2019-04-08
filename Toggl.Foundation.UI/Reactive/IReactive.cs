namespace Toggl.Foundation.UI.Reactive
{
    public interface IReactive<out TBase>
    {
        TBase Base { get; }
    }
}
