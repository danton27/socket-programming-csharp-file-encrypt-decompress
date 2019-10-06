using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;

class FileClientTcp
{
    public static string directoryPath = "/media/axcel/kuliah/Documents/c#/multithread/multithread-encrypt-compress-file/pathfile/";
    public static void Main(String[] args)
    {
        try
        {
            TcpClient client = new TcpClient("127.0.0.1", 9933);
            Console.WriteLine("Connected to server");

            // encrypt
            Console.WriteLine("Please enter a password to use:");
            string password = Console.ReadLine();
            Console.WriteLine("");

            // For additional security Pin the password of your files
            GCHandle gch = GCHandle.Alloc(password, GCHandleType.Pinned);

            Console.WriteLine("file name to encrypt:");
            string fileName = Console.ReadLine();
            string inputFile = fileName;
            Console.WriteLine(inputFile);

            // Encrypt the file
            FileEncrypt(inputFile , password);
            Console.WriteLine("checked");

            // To increase the security of the encryption, delete the given password from the memory !
            gch.Free();

            Console.WriteLine(directoryPath);

            //compress
            DirectoryInfo directorySelected = new DirectoryInfo(directoryPath);
            
            Compress(directorySelected);

            Console.WriteLine("Sending file.");  

            StreamWriter sWriter = new StreamWriter(client.GetStream());  

            byte[] bytes = File.ReadAllBytes(inputFile + ".aes" + ".gz");  

            sWriter.WriteLine(bytes.Length.ToString());  
            sWriter.Flush();  

            sWriter.WriteLine(inputFile + ".aes" + ".gz");  
            sWriter.Flush();  

            Console.WriteLine("Sending file");
            client.Client.SendFile(inputFile + ".aes" + ".gz");  
 

            
        } catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    //encrypt
    public static byte[] GenerateRandomSalt()
    {
        byte[] data = new byte[32];

        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            for (int i = 0; i < 10; i++)
            {
                // Fille the buffer with the generated data
                rng.GetBytes(data);
            }
        }

        return data;
    }


    public static void FileEncrypt(string inputFile, string password)
    {
        //generate random salt
        byte[] salt = GenerateRandomSalt();

        //create output file name
        FileStream fsCrypt = new FileStream(inputFile + ".aes", FileMode.Create);

        //convert password string to byte arrray
        byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

        //Set Rijndael symmetric encryption algorithm
        RijndaelManaged AES = new RijndaelManaged();
        AES.KeySize = 256;
        AES.BlockSize = 128;
        AES.Padding = PaddingMode.PKCS7;
        
        //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
        var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
        AES.Key = key.GetBytes(AES.KeySize / 8);
        AES.IV = key.GetBytes(AES.BlockSize / 8);

        
        AES.Mode = CipherMode.CFB;

        // write salt to the begining of the output file, so in this case can be random every time
        fsCrypt.Write(salt, 0, salt.Length);

        CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

        FileStream fsIn = new FileStream(inputFile, FileMode.Open);

        //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
        byte[] buffer = new byte[1048576];
        int read;

        try
        {
            while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
            {
                
                cs.Write(buffer, 0, read);
            }

            // Close up
            fsIn.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            cs.Close();
            fsCrypt.Close();
        }
        Console.WriteLine("file encrypted");
    }

    //compress
    public static void Compress(DirectoryInfo directorySelected)
    {
        foreach (FileInfo fileToCompress in directorySelected.GetFiles())
        {
            using (FileStream originalFileStream = fileToCompress.OpenRead())
            {
                if ((File.GetAttributes(fileToCompress.FullName) &
                    FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                {
                    using (FileStream compressedFileStream = File.Create(fileToCompress.FullName + ".gz"))
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                            CompressionMode.Compress))
                        {
                            originalFileStream.CopyTo(compressionStream);

                        }
                    }
                    FileInfo info = new FileInfo(directoryPath + Path.DirectorySeparatorChar + fileToCompress.Name + ".gz");
                    Console.WriteLine($"Compressed {fileToCompress.Name} from {fileToCompress.Length.ToString()} to {info.Length.ToString()} bytes.");
                }
            }
        }
    }
}