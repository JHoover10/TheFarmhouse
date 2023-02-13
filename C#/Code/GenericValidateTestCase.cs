using Newtonsoft.Json;
using Xunit.Abstractions;

namespace C_Sharp;

public class GenericValidateTestCase<T> : IXunitSerializable
{
    public string _TestName { get; set; }
    public T TestObject { get; set; }
    public Dictionary<string, object> Metadata { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        _TestName = info.GetValue<string>(nameof(_TestName));
        TestObject = JsonConvert.DeserializeObject<T>(info.GetValue<string>(nameof(_TestName)));
        Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(info.GetValue<string>(nameof(Metadata)));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(_TestName), _TestName);
        info.AddValue(nameof(TestObject), TestObject == null ? JsonConvert.SerializeObject(Activator.CreateInstance<T>()) : JsonConvert.SerializeObject(TestObject));
        info.AddValue(nameof(Metadata), Metadata == null ? JsonConvert.SerializeObject(new Dictionary<string, object>()) : JsonConvert.SerializeObject(Metadata));
    }
}