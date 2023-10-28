using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

[ApiController]
public class MemoryController : ControllerBase
{
    private readonly IKernel _kernel;

    public MemoryController(IKernel kernel)
    {
        _kernel = kernel;
    }

    [HttpPost("code")]
    public async Task<string> Store([FromBody] StoreCodeRequest request)
    {
        await _kernel.Memory.SaveInformationAsync("collection", request.CodeChunk, "id", request.Comment);
        return request.Comment;
    }
}

public class StoreCodeRequest
{
    public string Comment { get; set; }
    public string CodeChunk { get; set; }
}