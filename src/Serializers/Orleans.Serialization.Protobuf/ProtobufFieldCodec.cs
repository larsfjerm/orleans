using System;
using System.Buffers;
using Google.Protobuf;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Buffers.Adaptors;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.WireProtocol;

namespace Orleans.Serialization.Protobuf;

internal sealed class ProtobufFieldCodec : IFieldCodec<IMessage>
{
    private static readonly Type SelfType = typeof(ProtobufFieldCodec);

    private readonly MessageParser _messageParser;

    public ProtobufFieldCodec(MessageParser messageParser)
    {
        _messageParser = messageParser;
    }

    IMessage IFieldCodec<IMessage>.ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.IsReference)
        {
            return ReferenceCodec.ReadReference<IMessage, TInput>(ref reader, field);
        }

        field.EnsureWireTypeTagDelimited();

        var placeholderReferenceId = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
        ReferenceCodec.MarkValueField(reader.Session);
        var length = (int)reader.ReadVarUInt32();

        using var buffer = new PooledArrayBufferWriter();
        var spanBuffer = buffer.GetSpan(length)[..length];
        reader.ReadBytes(spanBuffer);
        var result = _messageParser.ParseFrom(spanBuffer);

        ReferenceCodec.RecordObject(reader.Session, result, placeholderReferenceId);
        return result;
    }

    public void WriteField<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, Type expectedType, IMessage value) where TBufferWriter : IBufferWriter<byte>
    {
        if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, value))
        {
            return;
        }

        writer.WriteFieldHeader(fieldIdDelta, expectedType, value.GetType(), WireType.TagDelimited);

        //// Write the type name
        //ReferenceCodec.MarkValueField(writer.Session);
        //writer.WriteFieldHeaderExpected(0, WireType.LengthPrefixed);
        //writer.Session.TypeCodec.WriteLengthPrefixed(ref writer, value.GetType());

        // Write the serialized payload
        var messageSize = value.CalculateSize();

        using var buffer = new PooledArrayBufferWriter();
        var spanBuffer = buffer.GetSpan(messageSize)[..messageSize];
        value.WriteTo(spanBuffer);

        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteVarUInt32((uint)spanBuffer.Length);
        writer.Write(spanBuffer);

        writer.WriteEndObject();
    }
}
