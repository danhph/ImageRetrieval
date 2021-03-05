using System.IO;
using FASTER.core;
using MsgPack.Serialization;

namespace ImageRetrieval.Core
{
    
    public class MessagePackFasterSerializer<T> : BinaryObjectSerializer<T>
    {
        private static readonly MessagePackSerializer<T> Serializer = MessagePackSerializer.Get<T>();

        public override void Deserialize(out T obj)
        {
            var count = reader.ReadInt32();
            var byteArray = reader.ReadBytes(count);
            using var ms = new MemoryStream(byteArray);
            obj = Serializer.Unpack(ms);
        }

        public override void Serialize(ref T obj)
        {
            using var ms = new MemoryStream();
            Serializer.Pack(ms, obj);
            writer.Write((int) ms.Position);
            writer.Write(ms.ToArray());
        }
    }
}