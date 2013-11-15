using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace StarBank
{
    //TODO: Check error codes
    /// <summary>
    /// A wrapper class for StormLib, a native DLL for reading MPQ files
    /// </summary>
    public class StormLibWrapper
    {
        #region P/invoke definitions
        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileOpenArchive(string mpqName, uint priority, uint flags, out IntPtr mpqHandle);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileCloseArchive(IntPtr mpqHandle);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileOpenFileEx(IntPtr mpqHandle, string fileName, uint searchScope,
                                                  out IntPtr fileHandle);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileReadFile(IntPtr fileHandle, byte[] buffer, uint bytesToRead, out uint bytesRead,
                                                IntPtr zero);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileCloseFile(IntPtr fileHandle);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileExtractFile(IntPtr mpqHandle, string fileToExtract, string pathToExtractTo, uint searchScope);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileHasFile(IntPtr mpqHandle, string fileName);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileCreateFile(IntPtr mpqHandle, string fileName, ulong dateTime, uint fileSize,
                                                   uint locale, uint flags, out IntPtr fileHandle);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileWriteFile(IntPtr fileHandle, byte[] data, uint dataSize, uint compression);

        [DllImport("StormLib", SetLastError = true)]
        private static extern bool SFileFinishFile(IntPtr fileHandle);

        [Flags]
        private enum OpenArchiveFlags
        {
            DontReadListFile = 0x00010000,
            DontReadAttributesFile = 0x00020000,
            VerifyCrc = 0x00080000,
            ReadOnly = 0x00000100
        }
        #endregion

        public class MpqArchive : IDisposable
        {
            private IntPtr _handle;

            public MpqArchive(string mpqArchivePath, bool isReadOnly = true)
            {
                uint flags = 0;
                if(isReadOnly)
                {
                    flags = (uint) (OpenArchiveFlags.DontReadListFile |
                                    OpenArchiveFlags.DontReadAttributesFile |
                                    OpenArchiveFlags.ReadOnly);
                }
                SFileOpenArchive(mpqArchivePath, 0, flags, out _handle);
            }

            /// <summary>
            /// Close the given archive.  All opened files must be closed first.
            /// </summary>
            public void Close()
            {
                if(_handle != IntPtr.Zero)
                {
                    SFileCloseArchive(_handle);
                    _handle = IntPtr.Zero;
                }
            }

            public void Dispose()
            {
                Close();
            }

            /// <summary>
            /// Opens a file from the archive for reading.
            /// Make sure to call Dispose() on the file when complete!
            /// </summary>
            public MpqInternalFile OpenFile(string fileName)
            {
                return new MpqInternalFile(fileName, _handle);
            }

            /// <summary>
            /// Extract a file from the archive onto the computer
            /// </summary>
            public void ExtractFile(string fileName, string outputPath)
            {
                SFileExtractFile(_handle, fileName, outputPath, 0);
            }

            public bool FileExists(string fileName)
            {
                return SFileHasFile(_handle, fileName);
            }

            public void CreateFile(string fileName, byte[] fileContents)
            {
                IntPtr fileHandle;
                SFileCreateFile(_handle, fileName, 0, (uint)fileContents.Length, 0, 0, out fileHandle);
                SFileWriteFile(fileHandle, fileContents, (uint)fileContents.Length, 0);
                SFileFinishFile(fileHandle);
            }
        }

        public class MpqInternalFile : IDisposable
        {
            private IntPtr _handle;

            public MpqInternalFile(string fileName, IntPtr mpqArchiveHandle)
            {
                SFileOpenFileEx(mpqArchiveHandle, fileName, 0, out _handle);
            }

            /// <summary>
            /// Close the given file.  Must be done before the archive can be closed.
            /// </summary>
            private void Close()
            {
                if(_handle != IntPtr.Zero)
                {
                    SFileCloseFile(_handle);
                    _handle = IntPtr.Zero;
                }
            }

            public void Dispose()
            {
                Close();
            }

            /// <summary>
            /// Read the given number of bytes from the file.
            /// If the file did not have that many bytes to read,
            /// the return value's length will be less-than numBytesToRead
            /// </summary>
            public byte[] Read(int numBytesToRead)
            {
                byte[] buffer = new byte[numBytesToRead];
                uint bytesRead;
                SFileReadFile(_handle, buffer, (uint)numBytesToRead, out bytesRead, IntPtr.Zero);

                //If we reached the end of the file, resize buffer
                if(bytesRead < numBytesToRead)
                {
                    byte[] newBuffer = new byte[bytesRead];
                    Array.Copy(buffer, newBuffer, bytesRead);
                    buffer = newBuffer;
                }

                return buffer;
            }

            /// <summary>
            /// Read the given number of characters as a string from the file.
            /// If the file did not have that many characters to read,
            /// the return value's length will be less-than numCharsToRead
            /// </summary>
            public string ReadString(int numCharsToRead)
            {
                return Encoding.ASCII.GetString(Read(numCharsToRead));
            }


            /// <summary>
            /// Reads the entire file into a string
            /// </summary>
            public string ReadFile()
            {
                const int bytesToReadAtOnce = 3000;
                List<string> stringsRead = new List<string>();
                string lastStringRead;
                do
                {
                    lastStringRead = ReadString(bytesToReadAtOnce);
                    stringsRead.Add(lastStringRead);
                } while (lastStringRead.Length == bytesToReadAtOnce);

                //Combine all the individually-read strings into one string
                return String.Join("", stringsRead);
            }
        }
    }
}
