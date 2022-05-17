using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace CLikeCompiler.Libs.Util
{
    public class TargetWriter
    {
        private static readonly TargetWriter Writer = new();
        private StorageFile codeFile;
        private readonly SemaphoreSlim semaphore;

        private TargetWriter()
        {
            semaphore = new SemaphoreSlim(1, 1);
        }

        public void Initialize()
        {
            OpenFileHandle();
        }

        public static TargetWriter Instance()
        {
            return Writer;
        }

        private async void OpenFileHandle()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            await semaphore.WaitAsync();
            codeFile = await storageFolder.CreateFileAsync("Code.txt",
                CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(codeFile, "");
            semaphore.Release();
        }

        public async void ExportTargetCodeToFile(List<string> code)
        {
            foreach (string str in code)
            {
                await semaphore.WaitAsync();
                await FileIO.AppendTextAsync(codeFile, str + "\n");
                semaphore.Release();
            }
        }

        public void OpenLogInNotepad()
        {
            System.Diagnostics.Process proc = new();
            proc.StartInfo.FileName = "notepad.exe";
            proc.StartInfo.Arguments = codeFile.Path;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
        }

        public async void ExportTargetCodeToFile(string code)
        {
            await semaphore.WaitAsync();
            await FileIO.AppendTextAsync(codeFile, code);
            semaphore.Release();
        }

        public async void ClearCodeFile()
        {
            await semaphore.WaitAsync();
            await FileIO.WriteTextAsync(codeFile, string.Empty);
            semaphore.Release();
        }
    }
}
