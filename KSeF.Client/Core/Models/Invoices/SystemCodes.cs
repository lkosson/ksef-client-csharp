using System.Runtime.Serialization;

namespace KSeF.Client.Core.Models.Invoices;

public enum SystemCodeEnum
{
    [EnumMember(Value = "FA (2)")] FA2,
    [EnumMember(Value = "FA (3)")] FA3,
    [EnumMember(Value = "FA_PEF (3)")] FAPEF,
    [EnumMember(Value = "FA_KOR_PEF (3)")] FAKORPEF
}

public static class SystemCodeHelper
{
    public static string GetSystemCode(SystemCodeEnum code) => code switch
    {
        SystemCodeEnum.FA2 => "FA (2)",
        SystemCodeEnum.FA3 => "FA (3)",
        SystemCodeEnum.FAPEF => "FA_PEF (3)",
        SystemCodeEnum.FAKORPEF => "FA_KOR_PEF (3)",
        _ => code.ToString()
    };

    public static string GetValue(SystemCodeEnum code) => code switch
    {
        SystemCodeEnum.FA2 => "FA",
        SystemCodeEnum.FA3 => "FA",
        SystemCodeEnum.FAPEF => "FA_PEF",
        SystemCodeEnum.FAKORPEF => "FA_PEF",
        _ => code.ToString()
    };

    public static string GetSchemaVersion(SystemCodeEnum code) => code switch
    {
        SystemCodeEnum.FA2 => "1-0E",
        SystemCodeEnum.FA3 => "1-0E",
        SystemCodeEnum.FAPEF => "2-1",
        SystemCodeEnum.FAKORPEF => "2-1",
        _ => code.ToString()
    };
}
