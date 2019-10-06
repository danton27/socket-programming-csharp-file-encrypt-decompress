using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace ConsoleServerApplication
{
    class ConnectionSetup
    {
        private TcpListener server = new TcpListener(IPAddress.Any, 9933);
        private bool waitingClientConnection = true;
        private TcpClient clientSocket = new TcpClient();
        
        public void StartServer()
        {
            // Start listening client request
            server.Start();
        
            Console.WriteLine("Listen at {0}", server.LocalEndpoint.ToString());
            while(waitingClientConnection)
            {
                try
                {
                    Console.WriteLine("Waiting Client Connection");
                    clientSocket = server.AcceptTcpClient();
                    ThreadHandler startFileTransfer = new ThreadHandler();
                    startFileTransfer.startThread(clientSocket);
                } catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static void Main(String[] args)
        {
            ConnectionSetup server = new ConnectionSetup();
            server.StartServer();
        }
    }

    public class ThreadHandler
    {
        private string directoryPath = "/media/axcel/kuliah/Documents/c#/multithread/multithread-encrypt-compress-file/serverpath/";

        private TcpClient clientSocket = null;

        public void startThread(TcpClient clientSocket)
        {
           this.clientSocket = clientSocket;
           Thread tcpThread = new Thread(new ThreadStart(threadProcess));
           tcpThread.Start();
        }

        public void threadProcess()
        {    
           StreamReader reader = new StreamReader(clientSocket.GetStream());

           string fileSize = reader.ReadLine();
           string fileName = reader.ReadLine();

           Int32 fileLength = Convert.ToInt32(fileSize);
           byte[] buffer = new byte[fileLength];
           Int32 received = 0;
           Int32 read = 0;
           Int32 size = 1024;
           Int32 remaining = 0;

            // Read bytes from the client using the length sent from the client    
            while (received < fileLength)  
            {  
                remaining = fileLength - received;  
                if (remaining < size)  
                {  
                    size = remaining;  
                }  

                read = clientSocket.GetStream().Read(buffer, received, size);  
                received += read;  
            }

            // Save the file using the filename sent by the client
            string outputPath = directoryPath + fileName;
            Console.WriteLine(outputPath);
            using (FileStream fStream = new FileStream(outputPath, FileMode.Create))
            {
                fStream.Write(buffer, 0, buffer.Length);
                fStream.Flush();
                fStream.Close();
            }


            Console.WriteLine("File received and saved in " + outputPath);

            //decompress
            DirectoryInfo directorySelected = new DirectoryInfo(directoryPath);
            foreach (FileInfo fileToDecompress in directorySelected.GetFiles("*.gz"))
            {
                Console.WriteLine(fileToDecompress);

                Decompress(fileToDecompress);
            }

            Console.WriteLine("Please enter a password to use:");
            string password = Console.ReadLine();

            // For additional security Pin the password of your files
            GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

            string inputFile = directoryPath + fileName;
            Console.WriteLine("Input File : {0}", inputFile);
            string outputFile =  directoryPath + "file.txt";
            Console.WriteLine("");

            // Decrypt the file
            FileDecrypt(inputFile, outputFile, password);
            Console.WriteLine("checked");

            // To increase the security of the decryption, delete the used password from the memory !

            gch.Free();

        }

        public void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
                    }
                }
            }
            Console.WriteLine("decompress jalan");
        }


        public void FileDecrypt(string inputFile, string outputFile, string password)
        {
            
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            
            fsCrypt.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CFB;

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);
            
            FileStream fsOut = new FileStream(outputFile, FileMode.Create);
            
            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
            Console.WriteLine("aman bos");
        }
    }
}
