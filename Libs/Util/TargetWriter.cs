using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using CLikeCompiler.Libs.Unit.Quads;

namespace CLikeCompiler.Libs.Util
{
    public class TargetWriter
    {
        private static readonly TargetWriter Writer = new();
        private StorageFile targetFile;
        private StorageFile midFile;
        private readonly SemaphoreSlim targetSemaphore;
        private readonly SemaphoreSlim midSemaphore;

        private TargetWriter()
        {
            targetSemaphore = new SemaphoreSlim(1, 1);
            midSemaphore = new SemaphoreSlim(1, 1);
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

            await targetSemaphore.WaitAsync();
            targetFile = await storageFolder.CreateFileAsync("Code.txt", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(targetFile, "");
            targetSemaphore.Release();

            await  midSemaphore.WaitAsync();
            midFile = await storageFolder.CreateFileAsync("Mid.txt", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(midFile, "");
            midSemaphore.Release();
        }

        public async void ExportMidCodeToFile(List<Quad> quadList)
        {
            StringBuilder builder = new();
            foreach (Quad quad in quadList)
            {
                builder.AppendLine(quad.ToString());
            }

            await midSemaphore.WaitAsync();
            await FileIO.WriteTextAsync(midFile, builder.ToString());
            midSemaphore.Release();
        }

        public void OpenFileInNotepad(bool isTarget)
        {
            System.Diagnostics.Process proc = new();
            proc.StartInfo.FileName = "notepad.exe";
            proc.StartInfo.Arguments = isTarget? targetFile.Path : midFile.Path;
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
        }

        public async void ExportTargetCodeToFile(string code)
        {
            await targetSemaphore.WaitAsync();
            await FileIO.AppendTextAsync(targetFile, code);
            targetSemaphore.Release();
        }

        public async void ClearCodeFile(bool isTarget)
        {
            if (isTarget)
            {
                await targetSemaphore.WaitAsync();
                await FileIO.WriteTextAsync(targetFile, string.Empty);
                targetSemaphore.Release();
            }
            else
            {
                await midSemaphore.WaitAsync();
                await FileIO.WriteTextAsync(midFile, string.Empty);
                midSemaphore.Release();
            }

        }
    }
}
