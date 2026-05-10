namespace Wolfgang.Conflict.Core.Tests.Unit;

public class AssemblyMarkerTests
{
    [Fact]
    public void AssemblyMarker_type_is_present_in_Core_assembly()
    {
        var marker = typeof(AssemblyMarker);

        Assert.Equal
        (
            "Wolfgang.Conflict.Core",
            marker.Assembly.GetName().Name
        );
    }
}
