using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;

namespace Sinawler
{
    public class Serialize
    {
        // 用于初始化对称密钥
        //private static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2 };
        static private String encryptKey = "sizheng0320Sinawler";

        //private static byte[] key = null;
        //private static byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };


        /// <summary>
        /// 将对象加密到字节数据
        /// </summary>
        /// <param name="obj">要加密的对象</param>
        /// <returns>处理后生成的字节数组</returns>
        public static byte[] EncryptToBytes ( object obj )
        {
            byte[] key = Encoding.ASCII.GetBytes( encryptKey.Substring( 0, 8 ) );
            byte[] IV = Encoding.ASCII.GetBytes( encryptKey );
            try
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream msPlaneText = new MemoryStream();

                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize( msPlaneText, obj );

                byte[] inputByteArray = msPlaneText.ToArray();
                msPlaneText.Close();
                MemoryStream msEncrypt = new MemoryStream();
                CryptoStream cs = new CryptoStream( msEncrypt, des.CreateEncryptor( key, IV ), CryptoStreamMode.Write );
                cs.Write( inputByteArray, 0, inputByteArray.Length );
                cs.FlushFinalBlock();
                byte[] byteEncrypt = msEncrypt.ToArray();
                cs.Close();
                return byteEncrypt;
            }
            catch (System.Exception error)
            {
                return null;
            }
        }

        /// <summary>
        /// 将字节数组进行解密还原成对象
        /// </summary>
        /// <param name="ary">要处理的字节数组</param>
        /// <returns>被还原的对象</returns>
        public static object DecryptToObject ( byte[] ary )
        {
            byte[] key = Encoding.ASCII.GetBytes( encryptKey.Substring( 0, 8 ) );
            byte[] IV = Encoding.ASCII.GetBytes( encryptKey );
            try
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream( ms, des.CreateDecryptor( key, IV ), CryptoStreamMode.Write );
                cs.Write( ary, 0, ary.Length );
                cs.FlushFinalBlock();
                cs.Close();

                byte[] byteDecrypt = ms.ToArray();
                MemoryStream msDecrypt = new MemoryStream( byteDecrypt );
                BinaryFormatter serializer = new BinaryFormatter();
                Object obj = serializer.Deserialize( msDecrypt );
                msDecrypt.Close();
                return obj;
            }
            catch (System.Exception error)
            {
                return null;
            }

        }

        /// <summary>
        /// 将对象压缩到字节数组
        /// </summary>
        /// <param name="obj">要压缩的对象</param>
        /// <returns>压缩后的字节数组</returns>
        public static byte[] CompressedToBytes ( object obj )
        {
            MemoryStream ms = new MemoryStream();
            DeflateStream zip = new DeflateStream( ms, CompressionMode.Compress, true );
            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize( zip, obj );
                zip.Close();
                byte[] ary = ms.ToArray();
                ms.Close();
                return ary;
            }
            catch (Exception e)
            {
                zip.Close();
                ms.Close();
                return null;
            }
        }

        /// <summary>
        /// 解压缩后对象
        /// </summary>
        /// <param name="ary">字节数组</param>
        /// <returns>对象</returns>
        public static object DecompressToObject ( byte[] ary )
        {
            MemoryStream ms = new MemoryStream( ary );
            DeflateStream UnZip = new DeflateStream( ms, CompressionMode.Decompress );
            try
            {
                BinaryFormatter serializer = new BinaryFormatter();
                object obj = serializer.Deserialize( UnZip );
                UnZip.Close();
                ms.Close();
                return obj;
            }
            catch (Exception e)
            {
                UnZip.Close();
                ms.Close();
                return null;
            }
        }
    }

}
