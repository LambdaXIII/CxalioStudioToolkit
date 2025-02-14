using Xunit;
using CxStudio;
using System.Collections.Generic;

namespace CxStudioTests
{
    public class TagReplacerTests
    {
        [Fact]
        public void InstallProvider_ShouldAddProvider()
        {
            var tagReplacer = new TagReplacer();
            var provider = new MockTagStringProvider();
            tagReplacer.InstallProvider("test", provider);

            var result = tagReplacer.ReplaceTags("${test}");
            Assert.Equal("mocked", result);
        }

        [Fact]
        public void ReplaceTags_ShouldReplaceSingleTag()
        {
            var tagReplacer = new TagReplacer();
            var provider = new MockTagStringProvider();
            tagReplacer.InstallProvider("test", provider);

            var result = tagReplacer.ReplaceTags("This is a ${test} tag.");
            Assert.Equal("This is a mocked tag.", result);
        }

        [Fact]
        public void ReplaceTags_ShouldReplaceMultipleTags()
        {
            var tagReplacer = new TagReplacer();
            var provider1 = new MockTagStringProvider();
            var provider2 = new MockTagStringProvider("another_mocked");
            tagReplacer.InstallProvider("test1", provider1);
            tagReplacer.InstallProvider("test2", provider2);

            var result = tagReplacer.ReplaceTags("This is a ${test1} and ${test2} tag.");
            Assert.Equal("This is a mocked and another_mocked tag.", result);
        }

        [Fact]
        public void ReplaceTags_ShouldReplaceTagWithParameter()
        {
            var tagReplacer = new TagReplacer();
            var provider = new MockTagStringProvider();
            tagReplacer.InstallProvider("test", provider);

            var result = tagReplacer.ReplaceTags("This is a ${test:param} tag.");
            Assert.Equal("This is a mocked:param tag.", result);
        }

        [Fact]
        public void ReplaceTags_ShouldHandleUnknownTags()
        {
            var tagReplacer = new TagReplacer();
            var provider = new MockTagStringProvider();
            tagReplacer.InstallProvider("test", provider);

            var result = tagReplacer.ReplaceTags("This is a ${unknown} tag.");
            Assert.Equal("This is a ${unknown} tag.", result);
        }

        private class MockTagStringProvider : ITagStringProvider
        {
            private readonly string _response;

            public MockTagStringProvider(string response = "mocked")
            {
                _response = response;
            }

            public string Replace(string? param)
            {
                return param != null ? $"{_response}:{param}" : _response;
            }
        }
    }
}
