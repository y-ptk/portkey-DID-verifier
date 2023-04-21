using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using CAVerifierServer.AccountAction;
using CAVerifierServer.Application;
using CAVerifierServer.Grains.Grain;
using CAVerifierServer.Options;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NUglify.Helpers;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace CAVerifierServer.Account;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class AccountAppService : CAVerifierServerAppService, IAccountAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ChainOptions _chainOptions;
    private readonly IEnumerable<IVerifyCodeSender> _verifyCodeSenders;
    private readonly WhiteListExpireTimeOptions _whiteListExpireTimeOptions;
    private readonly IDistributedCache<DidServerList> _distributedCache;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<AccountAppService> _logger;

    private const string CaServerListKey = "CAServerListKey";

    public AccountAppService(IClusterClient clusterClient,
        IOptions<ChainOptions> chainOptions,
        IDistributedCache<DidServerList> distributedCache,
        IEnumerable<IVerifyCodeSender> verifyCodeSenders, IObjectMapper objectMapper,
        IOptions<WhiteListExpireTimeOptions> whiteListExpireTimeOption, ILogger<AccountAppService> logger)
    {
        _clusterClient = clusterClient;
        _distributedCache = distributedCache;
        _verifyCodeSenders = verifyCodeSenders;
        _objectMapper = objectMapper;
        _logger = logger;
        _whiteListExpireTimeOptions = whiteListExpireTimeOption.Value;
        _chainOptions = chainOptions.Value;
    }

    public async Task<ResponseResultDto<SendVerificationRequestDto>> SendVerificationRequestAsync(
        SendVerificationRequestInput input)
    {
        var verifyCodeSender = _verifyCodeSenders.FirstOrDefault(v => v.Type == input.Type);
        if (verifyCodeSender == null)
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.Unsupported]
            };
        }

        if (!verifyCodeSender.ValidateGuardianIdentifier(input.GuardianIdentifier))
        {
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.Message[Error.InvalidGuardianIdentifierInput]
            };
        }

        try
        {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.GuardianIdentifier);
            var dto = await grain.GetVerifyCodeAsync(input);
            if (!dto.Success)
            {
                return new ResponseResultDto<SendVerificationRequestDto>
                {
                    Success = false,
                    Message = dto.Message
                };
            }

            await verifyCodeSender.SendCodeByGuardianIdentifierAsync(input.GuardianIdentifier, dto.Data.VerifierCode);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = true,
                Data = new SendVerificationRequestDto
                {
                    VerifierSessionId = input.VerifierSessionId
                }
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.SendVerificationRequestErrorLogPrefix + e.Message);
            return new ResponseResultDto<SendVerificationRequestDto>
            {
                Success = false,
                Message = Error.SendVerificationRequestErrorLogPrefix + e.Message
            };
        }
    }


    public async Task<ResponseResultDto<VerifierCodeDto>> VerifyCodeAsync(VerifyCodeInput input)
    {
        if (input.VerifierSessionId == Guid.Empty ||
            input.Code.IsNullOrEmpty() ||
            input.GuardianIdentifier.IsNullOrEmpty() ||
            input.Salt.IsNullOrEmpty() ||
            input.GuardianIdentifierHash.IsNullOrEmpty()
            )
        {
            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = Error.Message[Error.NullOrEmptyInput]
            };
        }

        try
        {
            var grain = _clusterClient.GetGrain<IGuardianIdentifierVerificationGrain>(input.GuardianIdentifier);
            var resultDto = await grain.VerifyAndCreateSignatureAsync(input);
            if (resultDto.Success)
            {
                return new ResponseResultDto<VerifierCodeDto>
                {
                    Success = true,
                    Data = new VerifierCodeDto
                    {
                        VerificationDoc = resultDto.Data.Data,
                        Signature = resultDto.Data.Signature
                    }
                };
            }

            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = resultDto.Message
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, Error.VerifyCodeErrorLogPrefix + e.Message);
            return new ResponseResultDto<VerifierCodeDto>
            {
                Success = false,
                Message = Error.VerifyCodeErrorLogPrefix + e.Message
            };
        }
    }

    public async Task<string> WhiteListCheckAsync(List<string> ipList)
    {
        var didServerList = await GetDidServerListAsync();
        if (didServerList == null)
        {
            throw new UserFriendlyException("No CAServer is Found");
        }
        var servers = didServerList.DidServers.Distinct().ToList();
        var endPoints = new List<string>();
        servers.ForEach(t => { endPoints.Add(t.EndPoint); });
        _logger.LogDebug("CaServerIPList id {ipList} :", string.Join(",", endPoints));
        var caIpList = endPoints.Select(ip => ip.Split("//")[1]).Select(formatter => formatter.Split(":")[0]).ToList();
        _logger.LogDebug("Formatter ipList is {caIpList}",string.Join(",",caIpList));
        var result = ipList.Intersect(caIpList).ToList();
        return result.Count==0?null:result[0];
        

    }


    private async Task<DidServerList> GetDidServerListAsync()
    {
        return await _distributedCache.GetOrAddAsync(
            CaServerListKey,
            async () => await GetCaServerListAsync(),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_whiteListExpireTimeOptions.ExpireTime)
            }
        );
    }

    private async Task<DidServerList> GetCaServerListAsync()
    {
        var didServerList = new DidServerList();
        var didServers = new List<DidServer>();
        foreach (var chainInfo in _chainOptions.ChainInfos)
        {
            var output = await GetCaServersListAsync(chainInfo.Value);
            var servers = output.CaServers;
            servers.ForEach(t =>
            {
                var didServer = _objectMapper.Map<CAServer, DidServer>(t);
                didServers.Add(didServer);
            });
            didServerList.DidServers = didServers;
        }

        return didServerList;
    }

    private async Task<GetCAServersOutput> GetCaServersListAsync(ChainInfo chainInfo)
    {
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
        var methodName = "GetCAServers";
        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainInfo.ContractAddress,
            methodName, param);
        var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        return GetCAServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }
}