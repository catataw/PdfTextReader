﻿using System;

namespace ParserConsoleWeb
{
    class Program
    {
        static void Main(string[] args)
        {
            if( args.Length == 0 )
            {
                Console.WriteLine("Syntax: ParserConsoleWeb <inputfile> [storage_account]");
                Console.WriteLine(" - inputile: does not include the extension");
                Console.WriteLine(" - storage_account: also configurable in Environment: STORAGE_ACCOUNT");
                return;
            }

            string inputFile = args[0];
            string storageAccount = (args.Length > 1) ? args[1] : Environment.GetEnvironmentVariable("STORAGE_ACCOUNT");

            var azureFS = new AzureFS(storageAccount);

            var pdf = new PdfProcessor(azureFS);

            string basename = GetBasename(inputFile);

            Console.WriteLine($"Baseline = {basename}");

            pdf.Process(basename);
        }

        static string GetBasename(string filename)
        {
            string[] components = filename.Split("/");

            string basename = components[components.Length-1];

            if (basename.ToLower().EndsWith(".pdf"))
                return basename.Substring(0, basename.Length - 4);

            return basename;
        }
    }
}
