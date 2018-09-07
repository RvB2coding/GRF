﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zlib;

namespace GRF
{
    public class GRF
    {
        private Stream _stream;
        private byte[] _encryptionKey;
        private int _fileTableOffset;
        private int _m1;
        private int _m2;
        private int _version;
        private int _compressedLength;
        private int _uncompressedLength;
        private byte[] _bodyBytes;
        private List<object> _files = new List<object>();

        public bool IsOpen { get; private set; } = false;
        public List<string> FileNames { get; } = new List<string>();
        public string Signature { get; private set; } = string.Empty;
        public int FileCount => _files.Count;

        public void Open( string filePath )
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var absolutePath = Path.Combine( baseDirectory, filePath );
            if( !File.Exists( absolutePath ) )
                throw new FileNotFoundException( filePath );

            _stream = new MemoryStream( File.ReadAllBytes( absolutePath ) );
            var streamReader = new BinaryReader( _stream );

            var signatureBytes = streamReader.ReadBytes( 15 );
            Signature = Encoding.ASCII.GetString( signatureBytes );
            streamReader.ReadByte(); // string null terminator

            _encryptionKey = streamReader.ReadBytes( 14 );
            _fileTableOffset = streamReader.ReadInt32();
            _m1 = streamReader.ReadInt32();
            _m2 = streamReader.ReadInt32();
            _version = streamReader.ReadInt32();

            _stream.Seek( _fileTableOffset, SeekOrigin.Current );

            _compressedLength = streamReader.ReadInt32();
            _uncompressedLength = streamReader.ReadInt32();

            var compressedBodyBytes = streamReader.ReadBytes( _compressedLength );
            _bodyBytes = ZlibStream.UncompressBuffer( compressedBodyBytes );

            var bodyStream = new MemoryStream( _bodyBytes );
            var bodyReader = new BinaryReader( bodyStream );

            var fileCount = _m2 - _m1 - 7;
            for( int i = 0; i < fileCount; i++ )
            {
                _files.Add( null );
            }

            IsOpen = true;
        }

        public void Close()
        {
            if( IsOpen )
            {
                _stream.Close();
                Signature = string.Empty;
                _files.Clear();
            }

            IsOpen = false;
        }
    }
}
