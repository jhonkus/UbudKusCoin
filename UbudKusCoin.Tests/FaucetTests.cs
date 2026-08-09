using System;
using System.Threading;
using System.Threading.Tasks;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using Xunit;
using Key = NBitcoin.Key;

namespace UbudKusCoin.Tests;

public class FaucetTests : IDisposable
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

    public FaucetTests()
    {
        // Ensure state is clean before test runs
        FaucetService.ResetRateLimits();
    }

    public void Dispose()
    {
        FaucetService.ResetRateLimits();
    }

    private static Key MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new Key(bytes);
    }

    [Fact]
    public void Faucet_GetAddress_ReturnsValidAddressString()
    {
        var addr = FaucetService.GetFaucetAddress();
        Assert.NotEmpty(addr);
        Assert.True(Address.TryParse(addr, out _));
    }

    [Fact]
    public async Task Faucet_Claim_EnforcesRateLimitsOnIpAndAddress()
    {
        // Mock application state machine environment
        var stakerKey = MakeKey(0x10);
        var stakerAddr = Address.FromPublicKey(Address.TestnetVersion, stakerKey.PubKey.ToBytes());
        var validatorKey = MakeKey(0x11);
        var validatorAddr = Address.FromPublicKey(Address.TestnetVersion, validatorKey.PubKey.ToBytes());

        var state = new State(ChainId);
        // Fund the faucet's address (using the node's local wallet address from WalletService)
        var faucetAddressText = FaucetService.GetFaucetAddress();
        var faucetAddr = Address.Parse(faucetAddressText);

        state.EnsureAccount(faucetAddr).Balance = Money.FromCoins(100m);
        state.EnsureAccount(stakerAddr);
        state.EnsureAccount(validatorAddr);

        var consensusOptions = ConsensusEngineOptions.FromEnvironment();
        // Since we are running in unit tests, we'll set up the ServicePool state machine mock
        var servicePoolType = typeof(ServicePool);
        var stateMachineField = servicePoolType.GetProperty("ApplicationStateMachine");
        Assert.NotNull(stateMachineField);

        var validatorSet = ConsensusValidatorConfig.Load(ChainId, ServicePool.WalletService, validatorKey.PubKey.ToBytes());
        var nodeService = new CanonicalNodeService(ChainId, "DbFiles/test-faucet-chain.json", validatorSet, null);
        
        // Update chain state to have funded faucet
        nodeService.Chain.State.EnsureAccount(faucetAddr).Balance = Money.FromCoins(100m);

        var stateMachine = new ConsensusApplicationStateMachine(
            nodeService.Chain.State,
            validatorAddr,
            validatorPublicKey: validatorKey.PubKey.ToBytes());
        stateMachineField.SetValue(null, stateMachine);

        try
        {
            // First claim should succeed (using valid math captcha fallback token)
            var result1 = await FaucetService.ClaimAsync(
                stakerAddr.Encoded,
                "192.168.1.50",
                "faucet-math-verified",
                CancellationToken.None);

            Assert.True(result1.Success, $"Expected claim 1 to succeed but got: {result1.Error}");
            Assert.Equal(FaucetService.ClaimAmount, result1.Amount);
            Assert.NotEmpty(result1.TxId!);

            // Second claim from the same IP should fail immediately due to rate limit
            var result2 = await FaucetService.ClaimAsync(
                Address.FromPublicKey(Address.TestnetVersion, MakeKey(0x20).PubKey.ToBytes()).Encoded,
                "192.168.1.50",
                "faucet-math-verified",
                CancellationToken.None);

            Assert.False(result2.Success);
            Assert.Contains("rate limited", result2.Error, StringComparison.OrdinalIgnoreCase);

            // Third claim to the same recipient address from a different IP should also fail
            var result3 = await FaucetService.ClaimAsync(
                stakerAddr.Encoded,
                "192.168.1.51",
                "faucet-math-verified",
                CancellationToken.None);

            Assert.False(result3.Success);
            Assert.Contains("rate limited", result3.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Clear test-specific state machine
            stateMachineField.SetValue(null, null);
        }
    }

    [Fact]
    public async Task Faucet_Claim_RejectsInvalidCaptchaToken()
    {
        var recipient = Address.FromPublicKey(Address.TestnetVersion, MakeKey(0x05).PubKey.ToBytes()).Encoded;
        var result = await FaucetService.ClaimAsync(
            recipient,
            "127.0.0.1",
            "invalid-token",
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Verification required", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
