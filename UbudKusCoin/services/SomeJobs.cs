using System.Threading.Tasks;
using Coravel.Invocable;
using  UbudKusCoin.Services;

namespace UbudKusCoin.Services
{
    public class SomeJobs : IInvocable
    {
        public Task Invoke()
        {
            // Blockchain.BuildNewBlock();
            // ReportsService.BuildReport();
            return Task.CompletedTask;
        }
    }
}