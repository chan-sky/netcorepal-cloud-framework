using NetCorePal.Extensions.Domain.Json;
using System.Text.Json;

namespace NetCorePal.Extensions.Domain.Abstractions.UnitTests
{
    public class EntityIdJsonConverterTests
    {
        [Fact]
        public void Serialize_Deserialize_Test()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new EntityIdJsonConverterFactory());

            var id = JsonSerializer.Deserialize<OrderId1>("\"12\"", options);
            Assert.NotNull(id);
            Assert.True(id.Id == 12);
            var json = JsonSerializer.Serialize(id, options);
            Assert.Equal("\"12\"", json);
            id = JsonSerializer.Deserialize<OrderId1>("16", options);
            Assert.NotNull(id);
            Assert.Equal(16, id.Id);

            Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<OrderId1>("\"\"", options));

            var id2 = JsonSerializer.Deserialize<OrderId2>("\"2\"", options);
            Assert.NotNull(id2);
            Assert.Equal(2, id2.Id);
            json = JsonSerializer.Serialize(id2, options);
            Assert.Equal("2", json);
            id2 = JsonSerializer.Deserialize<OrderId2>("5", options);
            Assert.NotNull(id2);
            Assert.Equal(5, id2.Id);

            Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<OrderId2>("\"\"", options));

            var id3 = JsonSerializer.Deserialize<OrderId3>("\"abc\"", options);
            Assert.NotNull(id3);
            Assert.Equal("abc", id3.Id);
            json = JsonSerializer.Serialize(id3, options);
            Assert.Equal("\"abc\"", json);

            var emptyId3 = JsonSerializer.Deserialize<OrderId3>("\"\"", options);
            Assert.NotNull(emptyId3);
            Assert.Equal(string.Empty, emptyId3.Id);
            
            var id4 = JsonSerializer.Deserialize<OrderId4>("\"0f8a7a4d-4a3d-4d3d-8d3a-3d4a7a0f8a7a\"", options);
            Assert.NotNull(id4);
            Assert.Equal("0f8a7a4d-4a3d-4d3d-8d3a-3d4a7a0f8a7a", id4.Id.ToString());
            json = JsonSerializer.Serialize(id4, options);
            Assert.Equal("\"0f8a7a4d-4a3d-4d3d-8d3a-3d4a7a0f8a7a\"", json);

            var defaultId4 = JsonSerializer.Deserialize<OrderId4>("\"00000000-0000-0000-0000-000000000000\"", options);;
            Assert.NotNull(defaultId4);
            Assert.Equal(Guid.Empty, defaultId4.Id);

            Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<OrderId4>("\"\"", options));
        }

        [Fact]
        public void JsonSerializer_Test()
        {
            JsonSerializerOptions options = new();
            options.Converters.Add(new EntityIdJsonConverterFactory());

            OrderId1? id1 = new OrderId1(10);
            var json = JsonSerializer.Serialize(id1, options);
            Assert.Equal("\"10\"", json);

            OrderId1? id1_null = null;
            json = JsonSerializer.Serialize(id1_null, options);
            Assert.Equal("null", json);

            OrderId2? id2 = new OrderId2(20);
            json = JsonSerializer.Serialize(id2, options);
            Assert.Equal("20", json);

            OrderId2? id2_null = null;
            json = JsonSerializer.Serialize(id2_null, options);
            Assert.Equal("null", json);

            OrderId3? id3 = new OrderId3("test");
            json = JsonSerializer.Serialize(id3, options);
            Assert.Equal("\"test\"", json);

            OrderId3? id3_null = null;
            json = JsonSerializer.Serialize(id3_null, options);
            Assert.Equal("null", json);

            OrderId3? id3_empty = new OrderId3(string.Empty);
            json = JsonSerializer.Serialize(id3_empty, options);
            Assert.Equal("\"\"", json);

            OrderId4? id4 = new OrderId4(Guid.Parse("0f8a7a4d-4a3d-4d3d-8d3a-3d4a7a0f8a7a"));
            json = JsonSerializer.Serialize(id4, options);
            Assert.Equal("\"0f8a7a4d-4a3d-4d3d-8d3a-3d4a7a0f8a7a\"", json);

            OrderId4? id4_null = null;
            json = JsonSerializer.Serialize(id4_null, options);
            Assert.Equal("null", json);
        }
    }
}