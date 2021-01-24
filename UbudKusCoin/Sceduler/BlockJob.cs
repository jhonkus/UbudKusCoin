using System.Threading.Tasks;
using Coravel.Invocable;
using Main;

namespace UbudKusCoin.Sceduler
{
    public class BlockJob : IInvocable
    {
        public Task Invoke()
        {
            Blockchain.BuildNewBlock();
            return Task.CompletedTask;
        }
    }
}