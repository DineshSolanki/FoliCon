using FoliCon.Models.Data.Wrapper;

namespace FoliCon.Models.Data;

public class ResultResponse
{
    public IResult Result { get; set; }
    public string MediaType { get; set; }
}