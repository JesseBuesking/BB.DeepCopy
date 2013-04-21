using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using BB.DeepCopy;

namespace AssemblyToProcess
{
    public static class DeepCopier
    {
        [DeepCopyMethod]
        public static T DeepCopy<T>(this T toBeCopied)
        {
            return default(T);
        }

        public static T DeepCopy2<T>(this T source)
        {
            return source.DeepCopy();
        }

        /// <summary>
        /// Performs a 'clone' (serialized copy) of the object.
        /// <para>http://stackoverflow.com/a/78612/435460</para>
        /// </summary>
        /// <typeparam name="T">The type of the object being cloned.</typeparam>
        /// <param name="source">The object instance to be cloned.</param>
        /// <returns>A cloned instance of the object.</returns>
        public static T Clone<T>(this T source)
        {
            // Doesn't work when the type is an interface.
            //            if (!typeof (T).IsSerializable)
            //                throw new ArgumentException("The type must be serializable.", "source");

            // Don't serialize null objects.
            if (ReferenceEquals(source, null))
                return default(T);

            try
            {
                using (var stream = new MemoryStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, source);
                    stream.Seek(0, SeekOrigin.Begin);
                    return (T) formatter.Deserialize(stream);
                }
            }
            catch (Exception)
            {
                throw;
//                return default(T);
            }
        }
    }
}