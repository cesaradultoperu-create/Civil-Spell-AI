// Both builds retain the same JSON files without requiring extra runtime DLLs.
sealed class JsonCodec
{
#if CAD2025
    public string Serialize(object value) => System.Text.Json.JsonSerializer.Serialize(value, value.GetType());
    public T Deserialize<T>(string value) => System.Text.Json.JsonSerializer.Deserialize<T>(value);
#else
    readonly System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
    public string Serialize(object value) => serializer.Serialize(value);
    public T Deserialize<T>(string value) => serializer.Deserialize<T>(value);
#endif
}
