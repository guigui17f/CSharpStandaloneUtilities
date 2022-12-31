using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace GUIGUI17F
{
    public class ProcessWatchClient
    {
        public Process OwnerProcess { get; private set; }
        public string ExtraParameter { get; private set; }
        public AutoResetEvent ResetEvent { get; private set; }

        private string _pipeName = "GUIGUI17F.ProcessWatcher";
        private string _key = "vOiyw4jkDGA4JNka8fBcA1aVnb20F+q3P4Fkz5a6Tn4=";
        private string _iv = "hAZQBNV4T0tvq+5I28Mqhw==";
        private string _ownerVerification = "Owner Verification";
        private string _clientVerification = "Client Verification";

        public bool Initialize()
        {
            bool success = false;
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Anonymous))
                {
                    pipeClient.Connect(800);
                    if (pipeClient.IsConnected)
                    {
                        using (StreamReader sr = new StreamReader(pipeClient))
                        {
                            using (StreamWriter sw = new StreamWriter(pipeClient))
                            {
                                sw.AutoFlush = true;
                                using (SymmetricAlgorithmHelper algorithmHelper = new SymmetricAlgorithmHelper(SymmetricAlgorithmHelper.AlgorithmName.AES, Convert.FromBase64String(_key), Convert.FromBase64String(_iv), true))
                                {
                                    if (algorithmHelper.Decrypt(sr.ReadLine(), Encoding.UTF8) == _ownerVerification)
                                    {
                                        sw.WriteLine(algorithmHelper.Encrypt(_clientVerification, Encoding.UTF8));
                                        string pId = algorithmHelper.Decrypt(sr.ReadLine(), Encoding.UTF8);
                                        ExtraParameter = algorithmHelper.Decrypt(sr.ReadLine(), Encoding.UTF8);
                                        OwnerProcess = Process.GetProcessById(int.Parse(pId));
                                        success = true;
                                    }
                                }
                            }
                        }
                    }
                    pipeClient.Close();
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
            OwnerProcess.EnableRaisingEvents = true;
            if (exitedHandler != null)
            {
                OwnerProcess.Exited += exitedHandler;
            }
            if (disposedHandler != null)
            {
                OwnerProcess.Disposed += disposedHandler;
            }
            if (outputDataReceivedHandler != null)
            {
                OwnerProcess.OutputDataReceived += outputDataReceivedHandler;
            }
            if (errorDataReceivedHandler != null)
            {
                OwnerProcess.ErrorDataReceived += errorDataReceivedHandler;
            }
        }

        public void SuspendThread()
        {
            if (ResetEvent == null)
            {
                ResetEvent = new AutoResetEvent(false);
            }
            else
            {
                ResetEvent.Reset();
            }
            ResetEvent.WaitOne();
        }

        public void ResumeThread()
        {
            if (ResetEvent != null)
            {
                ResetEvent.Set();
            }
        }
    }
}