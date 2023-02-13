using Newtonsoft.Json;
using Xunit.Abstractions;

namespace C_Sharp;

public class GenericValidateTestCase<T> : IXunitSerializable
{
    public string TestName { get; set; }
    public T TestObject { get; set; }
    public Dictionary<string, object> Metadata { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        TestName = info.GetValue<string>(nameof(TestName));
        TestObject = JsonConvert.DeserializeObject<T>(info.GetValue<string>(nameof(TestName)));
        Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(info.GetValue<string>(nameof(Metadata)));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(TestName), TestName);
        info.AddValue(nameof(TestObject), TestObject == null ? Activator.CreateInstance<T> : JsonConvert.SerializeObject(TestObject));
        info.AddValue(nameof(Metadata), Metadata == null ? new Dictionary<string, object>() : JsonConvert.SerializeObject(Metadata));
    }
}