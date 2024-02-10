using FoliCon.Models.Enums;

namespace FoliCon.Models.Data;

public record ParsedTitle(string Title, IdType IdType, string Id, int Year);