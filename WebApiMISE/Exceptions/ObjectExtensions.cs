namespace WebApiMISE.Exceptions
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T?> GetPropertiesOfType<T>(this object obj) where T : class
        {
            foreach (var property in obj.GetType().GetProperties().Where(prop => typeof(T).IsAssignableFrom(prop.PropertyType) && prop.GetValue(obj) != null))
            {
                yield return property.GetValue(obj) as T;
            }

            foreach (var property in obj.GetType().GetProperties().Where(prop => typeof(IEnumerable<T>).IsAssignableFrom(prop.PropertyType) && prop.GetValue(obj) != null))
            {
                foreach (var item in (IEnumerable<T>)property.GetValue(obj)!)
                {
                    yield return item;
                }
            }
        }
    }
}
