#if DEBUG
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Anteater.Intercom.HotReloadService))]
#endif

namespace Anteater.Intercom;

public static class HotReloadService
{
    public static event Action<Type[]> UpdateApplicationEvent;

    internal static void ClearCache(Type[] types) { }

    internal static void UpdateApplication(Type[] types)
    {
        UpdateApplicationEvent?.Invoke(types);
    }
}
