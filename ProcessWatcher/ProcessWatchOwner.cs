using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace GUIGUI17F
{
    public class ProcessWatchOwner
    {
        public Process ClientProcess { get; private set; }
        private string _pipeName = "GUIGUI17F.ProcessWatcher";
        private string _key = "vOiyw4jkDGA4JNka8fBcA1aVnb20F+q3P4Fkz5a6Tn4=";
        private string _iv = "hAZQBNV4T0tvq+5I28Mqhw==";
        private string _ownerVerification = "Owner Verification";
        private string _clientVerification = "Client Verification";
        private string _clientPath;
        private string _clientHash;

        public bool Initialize(string clientPath, string clientHash)
        {
            if (!File.Exists(clientPath))
            {
                Console.WriteLine("client doesn't exist!");
                return false;
            }
            using (HashAlgorithmHelper algorithmHelper = new HashAlgorithmHelper(HashAlgorithmHelper.AlgorithmName.SHA256))
            {
                if (algorithmHelper.GetFileHash(clientPath) != clientHash)
                {
                    Console.WriteLine("client hash verification failed!");
                    return false;
                }
            }
            _clientPath = clientPath;
            _clientHash = clientHash;
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = Path.GetFullPath(_clientPath),
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            ClientProcess = new Process
            {
                StartInfo = info
            };
            return true;
        }

        public bool StartWatcher(string extraParameter)
        {
            bool success = false;
            try
            {
                ClientProcess.Start();
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message))
                {
                    pipeServer.WaitForConnection();
                    using (StreamReader sr = new StreamReader(pipeServer))
                    {
                        using (StreamWriter sw = new StreamWriter(pipeServer))
                        {
                            sw.AutoFlush = true;
                            using (SymmetricAlgorithmHelper algorithmHelper = new SymmetricAlgorithmHelper(SymmetricAlgorithmHelper.AlgorithmName.AES, Convert.FromBase64String(_key), Convert.FromBase64String(_iv), true))
                            {
                                sw.WriteLine(algorithmHelper.Encrypt(_ownerVerification, Encoding.UTF8));
                                if (algorithmHelper.Decrypt(sr.ReadLine(), Encoding.UTF8) == _clientVerification)
                                {
                                    sw.WriteLine(algorithmHelper.Encrypt(Process.GetCurrentProcess().Id.ToString(), Encoding.UTF8));
                                    sw.WriteLine(algorithmHelper.Encrypt(extraParameter, Encoding.UTF8));
                                    success = true;
                                }
                            }
                        }
                    }
                    pipeServer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                success = false;
            }
            return success;
        }

        public void RegisterProcessEvents(EventHandler exitedHandler, EventHandler disposedHandler, DataReceivedEventHandler outputDataReceivedHandler, DataReceivedEventHandler errorDataReceivedHandler)
        {
            ClientProcess.EnableRaisingEvents = true;
            if (exitedHandler != null)
            {
                ClientProcess.Exited += exitedHandler;
            }
            if (disposedHandler != null)
            {
                ClientProcess.Disposed += disposedHandler;
            }
            if (outputDataReceivedHandler != null)
            {
                ClientProcess.OutputDataReceived += outputDataReceivedHandler;
            }
            if (errorDataReceivedHandler != null)
            {
                ClientProcess.ErrorDataReceived += errorDataReceivedHandler;
            }
        }

        public void UnRegisterProcessEvents(EventHandler exitedHandler, EventHandler disposedHandler, DataReceivedEventHandler outputDataReceivedHandler, DataReceivedEventHandler errorDataReceivedHandler)
        {
            if (exitedHandler != null)
            {
                ClientProcess.Exited -= exitedHandler;
            }
            if (disposedHandler != null)
            {
                ClientProcess.Disposed -= disposedHandler;
            }
            if (outputDataReceivedHandler != null)
            {
                ClientProcess.OutputDataReceived -= outputDataReceivedHandler;
            }
            if (errorDataReceivedHandler != null)
            {
                ClientProcess.ErrorDataReceived -= errorDataReceivedHandler;
            }
        }

        /// <summary>
        /// this will trigger ClientProcess.Exited and ClientProcess.Disposed events
        /// </summary>
        public bool ResetWatcher()
        {
            if (ClientProcess != null)
            {
                if (!ClientProcess.HasExited)
                {
                    ClientProcess.Close();
                }
                ClientProcess.Dispose();
            }
            return Initialize(_clientPath, _clientHash);
        }
    }
}