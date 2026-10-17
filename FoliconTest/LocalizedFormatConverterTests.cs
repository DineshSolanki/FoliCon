#nullable enable
using System.Globalization;
using System.Windows;
using FoliCon.Modules.Convertor;

namespace FoliconTest;

public class LocalizedFormatConverterTests
{
    private readonly LocalizedFormatConverter _converter = new();

    [Fact]
    public void Convert_NullOrEmptyValues_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, _converter.Convert(null!, typeof(string), null!, CultureInfo.InvariantCulture));
        Assert.Equal(string.Empty, _converter.Convert(Array.Empty<object>(), typeof(string), null!, CultureInfo.InvariantCulture));
        Assert.Equal(string.Empty, _converter.Convert(new object[] { 123 }, typeof(string), null!, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_NoDelimiter_FormatsStandardString()
    {
        var values = new object[] { "Found: {0}", 5 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("Found: 5", result);
    }

    [Fact]
    public void Convert_SingularPattern_CountIsOne_SelectsSingular()
    {
        var values = new object[] { "{0} pasta|{0} pastas", 1 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("1 pasta", result);
    }

    [Fact]
    public void Convert_SingularPattern_CountIsZero_SelectsPlural()
    {
        var values = new object[] { "{0} pasta|{0} pastas", 0 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("0 pastas", result);
    }

    [Fact]
    public void Convert_SingularPattern_CountIsMultiple_SelectsPlural()
    {
        var values = new object[] { "{0} pasta|{0} pastas", 4 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("4 pastas", result);
    }

    [Fact]
    public void Convert_PartitiveTwoArgs_TotalIsOne_SelectsSingular()
    {
        var values = new object[] { "{0} de {1} sobreposição|{0} de {1} sobreposições", 1, 1 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("1 de 1 sobreposição", result);
    }

    [Fact]
    public void Convert_PartitiveTwoArgs_TotalIsMultiple_SelectsPlural()
    {
        var values = new object[] { "{0} de {1} sobreposição|{0} de {1} sobreposições", 1, 5 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("1 de 5 sobreposições", result);
    }

    [Fact]
    public void Convert_NumericFirstArgNonNumericSecondArg_SelectsBasedOnFirstArg()
    {
        var values = new object[] { "{0} item in {1}|{0} items in {1}", 1, "Folder" };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("1 item in Folder", result);
    }

    [Fact]
    public void Convert_MultipleNumericArgsBeforeNonNumeric_SelectsLastNumericArg()
    {
        var valuesPlural = new object[] { "{0} of {1} file in {2}|{0} of {1} files in {2}", 1, 5, "Folder" };
        var resultPlural = _converter.Convert(valuesPlural, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("1 of 5 files in Folder", resultPlural);

        var valuesSingular = new object[] { "{0} of {1} file in {2}|{0} of {1} files in {2}", 1, 1, "Folder" };
        var resultSingular = _converter.Convert(valuesSingular, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("1 of 1 file in Folder", resultSingular);
    }

    [Fact]
    public void Convert_DependencyPropertyUnsetValue_TreatedAsEmpty()
    {
        var values = new object[] { "Value: {0}", DependencyProperty.UnsetValue };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("Value: ", result);
    }

    [Fact]
    public void Convert_MalformedFormat_ReturnsFormatGracefully()
    {
        var values = new object[] { "{0 unclosed", 1 };
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        Assert.Equal("{0 unclosed", result);
    }
}
