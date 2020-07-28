using Microsoft.Graph;
using System;
using System.Threading.Tasks;

namespace OofHarvester
{
    public class GraphHelper
    {
        private static GraphServiceClient graphClient;
        public static void Initialize(IAuthenticationProvider authProvider)
        {
            graphClient = new GraphServiceClient(authProvider);
        }

        public static async Task<User> GetMeAsync()
        {
            try
            {
                // GET /me
                return await graphClient.Me.Request().GetAsync();
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting signed-in user: {ex.Message}");
                return null;
            }
        }

        public static async Task<IUserMailFoldersCollectionPage> GetMailFolders()
        {
            try
            {
                return await graphClient.Me.MailFolders.Request().GetAsync();
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting mail folders: {ex.Message}");
                return null;
            }
        }

        public static async Task<IMailFolderChildFoldersCollectionPage> GetChildFolders(MailFolder mailFolder)
        {
            try
            {
                return await graphClient.Me.MailFolders[mailFolder.Id].ChildFolders.Request().GetAsync();
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting child folders: {ex.Message}");
                return null;
            }
        }

        public static async Task<IMailFolderMessagesCollectionPage> GetMessages(MailFolder mailFolder)
        {
            try
            {
                return await graphClient.Me.MailFolders[mailFolder.Id].Messages.
                    Request().
                    Select("internetMessageHeaders, subject, bodyPreview, body").
                    GetAsync();
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting messages: {ex.Message}");
                return null;
            }
        }

        public static async Task<IMailFolderMessagesCollectionPage> GetNextMessages(IMailFolderMessagesCollectionPage messages)
        {
            try
            {
                if (messages.NextPageRequest == null)
                    return null;

                return await messages.NextPageRequest.GetAsync();
            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error getting messages: {ex.Message}");
                return null;
            }
        }

    }
}