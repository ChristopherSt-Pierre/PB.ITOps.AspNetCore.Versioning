using Xunit;

namespace PB.ITOps.AspNetCore.Versioning.Tests
{
    public class IntroducedApiVersionAttributeTests
    {
        [Theory]
        [InlineData(0,0, "0.0")]
        [InlineData(1,0, "1.0")]
        [InlineData(10,0, "10.0")]
        [InlineData(1,1, "1.1")]
        [InlineData(10,1, "10.1")]
        [InlineData(1,10, "1.10")]
        [InlineData(0,1, "0.1")]
        [InlineData(0,10, "0.10")]
        public void GivenAnIntroducedApiVersion_WhenInstantiated_ThenMajorVersionIsSet(ushort majorVersion,ushort minorVersion, string expected) 
        {
            var actual = new IntroducedInApiVersionAttribute(majorVersion,minorVersion);
            
            Assert.Equal(expected, actual.Version.ToString());
        }
    }
}