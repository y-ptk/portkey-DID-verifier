using System;
using AutoMapper;
using CAVerifierServer.Account;
using CAVerifierServer.Application;

namespace CAVerifierServer;

public class CAVerifierServerApplicationAutoMapperProfile : Profile
{
    public CAVerifierServerApplicationAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        
        CreateMap<CAServer, DidServer>();
        
        
        
    }
}
