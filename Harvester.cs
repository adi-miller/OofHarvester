using Microsoft.Graph;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OofHarvester
{
    public class Harvester
    {
        private IDictionary<int, string> hash = new Dictionary<int, string>();
        private Stopwatch networkTime = new Stopwatch();
        private Stopwatch runTime = new Stopwatch();
        private int totalCount = 0;

        public void Harvest()
        {
            Console.Write($"Checking file WRITE permissions...");
            SaveResults("test.txt");

            var appId = "4aa38aa2-cdcf-48e8-ac76-ba9ea0cbc06b";
            var scopesString = "Mail.ReadBasic;Mail.Read;Mail.ReadWrite;User.Read";
            var scopes = scopesString.Split(';');

            // Initialize the auth provider with values from appsettings.json
            var authProvider = new DeviceCodeAuthProvider(appId, scopes);

            // Request a token to sign in the user
            var accessToken = authProvider.GetAccessToken().Result;        

            Harvest(authProvider);
        }

        private void Harvest(IAuthenticationProvider authProvider)
        {
            // Initialize Graph client
            GraphHelper.Initialize(authProvider);

            // Get signed in user
            var user = GraphHelper.GetMeAsync().Result;
            Console.WriteLine($"\nWelcome {user.DisplayName}.\n\n"+
                            "Thanks for your participation. Starting to scan your mailbox now.\n"+
                            "Depending on the size of your mailbox, this may take a few hours.\n"+
                            "Feel free to minimize this window and check back later...\n");

            try 
            {
                runTime.Start();
                networkTime.Start();
                var folders = GraphHelper.GetMailFolders().Result;
                networkTime.Stop();
                foreach (var folder in folders)
                {
                    ProcessFolder(folder);
                }
            }
            finally
            {
                networkTime.Stop();
                var filename = $".\\OOF{Program.Ver}-{user.DisplayName}.txt";
                Console.Write($"Saving {hash.Count} results to '{filename}'...");
                SaveResults(filename);
                runTime.Stop();
                Console.WriteLine("\nI'm done. Thanks!\n"+
                                $"Total runtime: {runTime.Elapsed }\n"+
                                $"Network time: {networkTime.Elapsed}\n"+
                                $"Found {hash.Count} unique OOF messages out of {totalCount} total processed messages.");
            }
        }

        private void SaveResults(string filename)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                foreach (var item in hash.Values)
                {
                    file.WriteLine(item);
                    file.WriteLine(Program.EOMString);
                }
            }
            Console.WriteLine($" Done.");
        }

        private void ProcessFolder(MailFolder folder)
        {
    //            if (folder.DisplayName != "Deleted Items")
            {
                try
                {
                    ProcessMessages(folder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing messages in folder '{folder.DisplayName}': {ex.Message}");
                }
            }
            IMailFolderChildFoldersCollectionPage childFolders;

            try
            {
                networkTime.Start();
                childFolders = GraphHelper.GetChildFolders(folder).Result;
            } 
            finally 
            {
                networkTime.Stop();
            }

            foreach (var childFolder in childFolders)
            {
                try
                {
                    ProcessFolder(childFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing messages in child folder '{childFolder.DisplayName}': {ex.Message}");
                }
            }
        }

        private void ProcessMessages(MailFolder folder)
        {
            int count = 0;
            var percentDone = 0.0;
            Console.Write($"\rScanning folder '{folder.DisplayName}' [{percentDone:F1}%]...");
            try
            {
                IMailFolderMessagesCollectionPage messages;
                try
                {
                    networkTime.Start();
                    messages = GraphHelper.GetMessages(folder).Result;
                }
                finally
                {
                    networkTime.Stop();
                }

                while (messages != null)
                {
                    foreach (var message in messages)
                    {
                        count++;
                        percentDone = ((double)count / folder.TotalItemCount.Value) * 100.0;
                        Console.Write($"\rScanning folder '{folder.DisplayName}' [{percentDone:F1}%]...");
                        ProcessMessage(folder, message);
                    }
                    var nextPage = GraphHelper.GetNextMessages(messages);
                    if (nextPage != null)
                        try
                        {
                            networkTime.Start();
                            messages = nextPage.Result;
                        }
                        finally
                        {
                            networkTime.Stop();
                        }
                    else
                        messages = null;
                }
            }
            finally
            {
                totalCount = totalCount + count;
                Console.WriteLine($" Count check: {count} out of {folder.TotalItemCount}");
            }
        }

        private void ProcessMessage(MailFolder folder, Message message)
        {
            try
            {
                if (message.InternetMessageHeaders != null)
                {
                    var response = false;
                    var autogen = false;
                    foreach (var header in message.InternetMessageHeaders)
                    {
                        if (header.Name.Equals("X-Auto-Response-Suppress") && header.Value.Equals("All"))
                            response = true;
                        if (header.Name.Equals("Auto-Submitted") && header.Value.Equals("auto-generated"))
                            autogen = true;
                        if (response && autogen)
                        {
                            if (message.Body.Content.Contains("[CodeFlow]"))
                                break;

                            Console.WriteLine($"Found OOF message: '{message.BodyPreview}'");
                            var cleanMessage = Regex.Replace(message.Body.Content, @"<[^>]+>|&nbsp;", " ");
                            cleanMessage = Regex.Replace(cleanMessage, @"&quot;", "'");
                            cleanMessage = Regex.Replace(cleanMessage, @"\s{2,}", " ");
                            var hashCode = cleanMessage.GetHashCode();
                            if (!hash.ContainsKey(hashCode))
                            {
                                hash.Add(hashCode, cleanMessage);
                            }
                            else
                            {
                                Console.WriteLine($"Discarding (duplicate)...");
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }
    }
}
