using System;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;

using MonoTouch.Foundation;

namespace MonoTouchUtils
{
    /// <summary>
    /// Helper methods to serialize objects to and from UserDefaults as xml.
    /// </summary>
    public static class UserDefaultsUtils
    {
        /// <summary>
        /// Loads the object in StandardUserDefaults under the given key using an XmlSerializer.
        /// </summary>
        /// <returns>
        /// The deserialized object, or null if the key value was null.
        /// </returns>
        public static T LoadObject<T>(string key) {
            var data = NSUserDefaults.StandardUserDefaults[key] as NSData;
            if (null != data) {
                byte[] dataBytes = new byte[data.Length];

                System.Runtime.InteropServices.Marshal.Copy(data.Bytes, dataBytes, 0, Convert.ToInt32(data.Length));
                using (var reader = new System.IO.StreamReader(
                        new GZipStream(new MemoryStream(dataBytes), CompressionMode.Decompress))) {
                    var serializer = new XmlSerializer(typeof(T));
                    return (T)serializer.Deserialize(reader);
                }
            } else {
                return default(T);
            }
        }

        /// <summary>
        /// Saves an object in StandardUserDefaults under the given key using an XmlSerializer.
        /// </summary>
        public static void SaveObject(string key, object obj) {
            var serializer = new XmlSerializer(obj.GetType());
            using (var stream = new System.IO.MemoryStream()) {
                using (var writer = new System.IO.StreamWriter(new GZipStream(stream, CompressionMode.Compress))) {
                    serializer.Serialize(writer, obj);
                    writer.Close();
    
                    NSUserDefaults.StandardUserDefaults[key] = NSData.FromArray(stream.GetBuffer());
                    NSUserDefaults.StandardUserDefaults.Synchronize();
                }
            }
        }
    }
}

