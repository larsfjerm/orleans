using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Orleans.Serialization.Cloning;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Protobuf;
using Orleans.Serialization.Serializers;

namespace Orleans.Serialization;

[Alias(WellKnownAlias)]
public sealed class ProtobufCodec : ISpecializableCodec, ISpecializableCopier, ITypeFilter
{
    public const string WellKnownAlias = "protobuf";

    private readonly ICodecSelector[] _serializableTypeSelectors;
    private readonly ICopierSelector[] _copyableTypeSelectors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtobufCodec"/> class.
    /// </summary>
    /// <param name="serializableTypeSelectors">Filters used to indicate which types should be serialized by this codec.</param>
    /// <param name="copyableTypeSelectors">Filters used to indicate which types should be copied by this codec.</param>
    public ProtobufCodec(
        IEnumerable<ICodecSelector> serializableTypeSelectors,
        IEnumerable<ICopierSelector> copyableTypeSelectors)
    { 
        _serializableTypeSelectors = serializableTypeSelectors.Where(t => string.Equals(t.CodecName, WellKnownAlias, StringComparison.Ordinal)).ToArray();
        _copyableTypeSelectors = copyableTypeSelectors.Where(t => string.Equals(t.CopierName, WellKnownAlias, StringComparison.Ordinal)).ToArray();
    }

    /// <inheritdoc/>
    public IDeepCopier GetSpecializedCopier(Type type) =>
        (IDeepCopier)Activator.CreateInstance(typeof(ProtobufMessageCopier<>).MakeGenericType(type));

    /// <inheritdoc/>
    IFieldCodec ISpecializableCodec.GetSpecializedCodec(Type type)
    {
        if (Activator.CreateInstance(type) is not IMessage protobufMessageInstance)
        {
            throw new InvalidOperationException("not a message");
        }

        var protobufFieldCodec = new ProtobufFieldCodec(protobufMessageInstance.Descriptor.Parser);
        return protobufFieldCodec;
    }

    /// <inheritdoc/>
    bool ISpecializableCodec.IsSupportedType(Type type)
    {
        foreach (var selector in _serializableTypeSelectors)
        {
            if (selector.IsSupportedType(type))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    bool ISpecializableCopier.IsSupportedType(Type type)
    {
        foreach (var selector in _copyableTypeSelectors)
        {
            if (selector.IsSupportedType(type))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public bool? IsTypeAllowed(Type type) =>
        ((ISpecializableCodec)this).IsSupportedType(type) || ((ISpecializableCopier)this).IsSupportedType(type);
}