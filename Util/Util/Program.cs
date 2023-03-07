using ConsoleApp3;
using Microsoft.Graph;
using System.IO;

namespace Util
{
    internal class Program
    {
        private static string tenantId = "";
        private static string appId = "";
        private static string secret = "";
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello, World!");
                var test = new GraphClient(tenantId, appId, secret);
                var res = await test.AddServicePrincipal(
                    new ServicePrincipal() { DisplayName= "testmenow" }
                    ) ;

                Console.WriteLine(res);
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}