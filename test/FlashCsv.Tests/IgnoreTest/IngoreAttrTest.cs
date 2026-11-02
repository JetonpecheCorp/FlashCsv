using System.Text;
using FlashCsv.Core;

namespace FlashCsv.Tests.IgnoreTest;

public class IngoreAttrTest
{
    [Fact]
    public async Task Ignores_Field_Completely_When_CsvIgnore_Is_Used()
    {
        // Vérifie que les en-têtes ne contiennent pas LocalCache
        var headers = AccountRecord.GetHeaderNames();
        Assert.DoesNotContain("LocalCache", headers);

        string csv = 
            "id,is_admin,internal_notes\r\n" +
            "1,true,SuperAdminNotes\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<AccountRecord>();

        await foreach (var item in FlashCsvSerializer.DeserializeAsync<AccountRecord>(stream))
        {
            list.Add(item);
        }

        Assert.Single(list);
        Assert.Null(list[0].LocalCache);
        Assert.Equal("SuperAdminNotes", list[0].InternalNotes);
    }

    [Fact]
    public async Task Ignores_Field_Value_When_Condition_Is_Satisfied()
    {
        // is_admin = false -> InternalNotes doit être ignoré et rester null
        string csv = 
            "id,is_admin,internal_notes\r\n" +
            "2,false,SecretDataThatShouldBeIgnored\r\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var list = new List<AccountRecord>();

        await foreach (var item in FlashCsvSerializer.DeserializeAsync<AccountRecord>(stream))
        {
            list.Add(item);
        }

        Assert.Single(list);
        Assert.False(list[0].IsAdmin);
        Assert.Null(list[0].InternalNotes);
    }
}
