using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tests
{
    public static class DeepCopier
    {
        /// <summary>
        /// Performs a 'clone' (serialized copy) of the object.
        /// <para>http://stackoverflow.com/a/78612/435460</para>
        /// </summary>
        /// <typeparam name="T">The type of the object being cloned.</typeparam>
        /// <param name="source">The object instance to be cloned.</param>
        /// <returns>A cloned instance of the object.</returns>
        public static T Clone<T>(this T source)
        {
            if (ReferenceEquals(source, null))
                return default(T);

            using (var stream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, source);
                stream.Seek(0, SeekOrigin.Begin);
                return (T) formatter.Deserialize(stream);
            }
        }
    }
}