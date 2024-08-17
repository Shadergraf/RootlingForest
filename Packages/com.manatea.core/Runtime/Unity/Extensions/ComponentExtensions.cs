namespace UnityEngine
{
    public static class ComponentExtensions
    {
        public static T CopyComponent<T>(this T component, GameObject destination) where T : Component
        {
            System.Type type = component.GetType();
            var dst = destination.AddComponent(type) as T;
            var fields = type.GetFields();
            foreach (var field in fields)
            {
                if (field.IsStatic) continue;
                field.SetValue(dst, field.GetValue(component));
            }
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
                prop.SetValue(dst, prop.GetValue(component, null), null);
            }
            return dst;
        }
    }
}
