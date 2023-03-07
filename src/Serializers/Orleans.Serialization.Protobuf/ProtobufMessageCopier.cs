using Google.Protobuf;
using Orleans.Serialization.Cloning;

namespace Orleans.Serialization;

internal sealed class ProtobufMessageCopier<T> : IDeepCopier<IMessage<T>> where T : class, IMessage<T>
{
    public IMessage<T> DeepCopy(IMessage<T> input, CopyContext context) => input?.Clone();
}
