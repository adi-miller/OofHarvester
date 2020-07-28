using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace OofHarvester
{
    public class Collector
    {
        private static IDictionary<int, string> hash = new Dictionary<int, string>();
        public static void Collect(string pattern)
        {
            var totalCount = 0;

            DirectoryInfo folder = new DirectoryInfo(".");
            FileInfo[] files = folder.GetFiles(pattern);

            foreach (FileInfo file in files)
            {
                Console.Write($"Processing file '{file.FullName}'...");

                using (StreamReader stream = new StreamReader(file.FullName)) 
                {  
                    var sb = new StringBuilder();
                    var count = 0;
                    var dupCount = 0;
                    var codeFlow = false;

                    string line;
                    while ((line = stream.ReadLine()) != null) 
                    {
                        if (!line.Equals(Program.EOMString))
                        {
                            sb.Append(line);
                            if (line.Contains("[CodeFlow]"))
                                codeFlow = true;
                        }
                        else
                        {
                            count++;
                            var message = sb.ToString();
                            var hashCode = message.GetHashCode();
                            if (!codeFlow && !hash.ContainsKey(hashCode))
                            {
                                hash.Add(hashCode, message);
                            }
                            else
                            {
                                dupCount++;
                            }
                            sb.Clear();
                        }
                    }
                    Console.WriteLine($" Done. Added {count-dupCount} messages out of {count} processed...");
                    totalCount = totalCount + (count-dupCount);
                }
            }

            Console.WriteLine($"\nDone. File contains {totalCount} messages.");

            var filename = $".\\OOF{Program.Ver}.txt";
            var testCount = 0;
            using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(filename))
            {
                foreach (var item in hash.Values)
                {
                    outputFile.WriteLine(item);
                    outputFile.WriteLine(Program.EOMString);
                    testCount++;
                }
            }
            if (testCount != totalCount)
                Console.WriteLine(" Checksum error...");
        }
    }
}