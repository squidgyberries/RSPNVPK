﻿using System;
using System.Linq;
using System.IO;

namespace RSPNVPK
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Invalid usage...");
                return;
            }

            var silent = false;

            if(args.Length > 1)
            {
                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i] == "-s" || args[i] == "/s")
                        silent = true;
                }
            }

            var vpkdir = args[0];
            if(!vpkdir.EndsWith("_dir.vpk"))
            {
                Console.WriteLine($"Invalid directory file {vpkdir}");
                return;
            }

            var vpkBackup = vpkdir.Replace("_dir.vpk", "_BACKUP.vpk");
            var number = 0;
            while (File.Exists(vpkBackup))
            {
                number++;
                vpkBackup = vpkBackup.Replace("_BACKUP.vpk", $"_BACKUP({number}).vpk");
            }

            var vpkarch = vpkdir.Replace("_dir.vpk", "_228.vpk").Replace("english", "");
            var directory = vpkdir.Replace(".vpk", "")+"\\";

            Console.WriteLine($"VPK directory: {vpkdir}\n" +
                $"VPK archive: {vpkarch}\n" +
                $"Directory: {directory}");

            var filesEdit = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Select(path => path.Replace(directory, "").Replace('\\', '/')).ToList();
            foreach(var edit in filesEdit)
            {
                Console.WriteLine($"\t{edit}");
            }

            if (!silent)
            {
                Console.WriteLine(@"
 _____ _   _ ___ ____    _____ ___   ___  _
|_   _| | | |_ _/ ___|  |_   _/ _ \ / _ \| |
  | | | |_| || |\___ \    | || | | | | | | |
  | | |  _  || | ___) |   | || |_| | |_| | |___
  |_| |_| |_|___|____/    |_| \___/ \___/|_____|

 ____   ___  _____ ____    __  __    _    _  _______
|  _ \ / _ \| ____/ ___|  |  \/  |  / \  | |/ / ____|
| | | | | | |  _| \___ \  | |\/| | / _ \ | ' /|  _|
| |_| | |_| | |___ ___) | | |  | |/ ___ \| . \| |___
|____/ \___/|_____|____/  |_|  |_/_/   \_\_|\_\_____|

 ____    _    ____ _  ___   _ ____  ____  _ _ _
| __ )  / \  / ___| |/ / | | |  _ \/ ___|| | | |
|  _ \ / _ \| |   | ' /| | | | |_) \___ \| | | |
| |_) / ___ \ |___| . \| |_| |  __/ ___) |_|_|_|
|____/_/   \_\____|_|\_\\___/|_|   |____/(_|_|_)
");
                Console.WriteLine("Press Enter to continue");
                Console.ReadLine();
            }

            var fstream = new FileStream(vpkdir, FileMode.Open, FileAccess.ReadWrite);
            var k0k = new FileStream(vpkarch, FileMode.OpenOrCreate, FileAccess.Write);
            k0k.Position = 0;
            k0k.SetLength(0);

            var writer = new BinaryWriter(fstream);
            var vpk = new VPK.DirFile(fstream);
            Console.WriteLine($"{vpk.Header.DirectorySize:X4} | {vpk.Header.EmbeddedChunkSize:X4}");

            var list = vpk.EntryBlocks.ToList();

            for (var i = 0; i < vpk.EntryBlocks.Length; i++)
            {
                var block = list[i];
                string? kek = null;

                foreach (var edit in filesEdit)
                {
                    if (edit == block.Path)
                    {
                        Console.WriteLine($"Replacing {edit}...");

                        var fb = File.ReadAllBytes(directory + edit);
                        if (fb.Length == 0)
                            throw new Exception("Brih");

                        list[i] = new VPK.DirEntryBlock(fb, (ulong)k0k.Position, 228, 0x101, 0, block.Path);

                        k0k.Write(fb);
                        k0k.Flush();

                        kek = edit;
                        break;
                    }
                }

                if (kek != null)
                    filesEdit.Remove(kek);
            }

            // if there are still files left...
            foreach (var edit in filesEdit)
            {
                Console.WriteLine($"Adding {edit}...");

                var fb = File.ReadAllBytes(directory + edit);
                if (fb.Length == 0)
                    throw new Exception("Brih");

                list.Add(new VPK.DirEntryBlock(fb, (ulong)k0k.Position, 228, 0x101, 0, edit));

                k0k.Write(fb);
                k0k.Flush();
            }

            writer.BaseStream.Position = 0;
            VPK.DirFile.Write(writer, list.ToArray());

            Console.WriteLine("Done!\nPress Enter to exit!");
            if(!silent)
                Console.ReadLine();
        }
    }
}
