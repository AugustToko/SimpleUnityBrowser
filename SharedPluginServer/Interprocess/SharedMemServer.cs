﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedMemory;


namespace SharedPluginServer
{
    public class SharedMemServer:IDisposable
    {
        private SharedMemory.SharedArray<byte> _sharedBuf;

        private bool _isOpen;

        public string Filename;

        private static readonly log4net.ILog log =
   log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


       

        public void Init(int size,string filename)
        {
            _sharedBuf=new SharedArray<byte>(filename,size);
            _isOpen = true;
            Filename = filename;

        }

        public void Resize(int newSize)
        {


            if (_sharedBuf.Length != newSize)
            {
                _sharedBuf.Close();
                _sharedBuf = new SharedArray<byte>(Filename, newSize);
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            if (_isOpen)
            {
                if (bytes.Length > _sharedBuf.Length)
                {
                    Resize(bytes.Length);
                }
                _sharedBuf.Write(bytes);
            }
        }

        public void Dispose()
        {
            _isOpen = false;
            _sharedBuf.Close();
        }

      

      

    }

   

  

}
