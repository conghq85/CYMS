using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Tranbros.Sport
{
    public class BaseDTO
    {
        public string ID
        {
            get;
            set;
        }
        public static T DeepClone<T>(T obj)
        {
            T result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                try
                {
                    binaryFormatter.Serialize(memoryStream, obj);
                    memoryStream.Position = 0;
                    result = (T)binaryFormatter.Deserialize(memoryStream);
                }
                catch (Exception ex)
                {
                    result = default(T);
                }

            }
            return result;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
