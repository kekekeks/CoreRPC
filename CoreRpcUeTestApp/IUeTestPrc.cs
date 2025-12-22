using CoreRPC.AspNetCore;

namespace CoreRpcUeTestApp;

public class UeResponseDto
{
    public bool Success { get; set; }
}

public class UeStringResponseDto : UeResponseDto
{
    public string Result { get; set; }
}

public interface IUeTestPrc
{
    Task<UeResponseDto> Login(string login);
    Task<UeStringResponseDto> RequestInfo(int id);
}

[RegisterRpc(typeof(IUeTestPrc))]
public class UeTestRpc : IUeTestPrc
{
    public Task<UeResponseDto> Login(string login)
    {
        return Task.FromResult(new UeResponseDto() { Success = true });
    }

    public Task<UeStringResponseDto> RequestInfo(int id)
    {
        throw new InvalidOperationException("Exception");
    }
}