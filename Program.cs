using System;

namespace OofHarvester
{
    class Program
    {
        /// Version history
        // v0.08 - Fixed duplication check (Used the has of the original message in the Hastable instead of the clean one)
        // v0.09 - Fixed totalCount to get updated in the finally as well. Missed count of folders that failed in the middle due to an error
        //       - Check for file write permissions before starting
        //       - Only enumerates folders - doesn't process messages
        // v0.10 - Back to process messages as well.
        // v0.11 - Refactor to a class, added support for Collect()
        ///
        public static string Ver = "v0.11";
        public static string EOMString = "--- End of Message ---";

        static void Main(string[] args)
        {
            Console.WriteLine($"OOF Harveseter {Ver}. Hackathon 2020.\n");

            if (args.Length != 0)
                Collector.Collect(args[0]);
            else
            {
                var harvester = new Harvester();
                harvester.Harvest();
            }
        }
    }
}
