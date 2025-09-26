﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetCorePal.Extensions.Domain.Json
{
    public class EntityIdJsonConverterFactory : JsonConverterFactory
    {
        private static readonly ConcurrentDictionary<Type, JsonConverter> Cache = new();

        public override bool CanConvert(Type typeToConvert)
        {
            return Array.Exists(typeToConvert.GetInterfaces(), p => p.Name == (typeof(IStronglyTypedId<>).Name));
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return Cache.GetOrAdd(typeToConvert, CreateConverter);
        }

        private JsonConverter CreateConverter(Type typeToConvert)
        {
            var stronglyTypedIdTypeInterface = Array.Find(typeToConvert.GetInterfaces(), p => p.Name == (typeof(IStronglyTypedId<>).Name));
            if (stronglyTypedIdTypeInterface != null)
            {
                var type = typeof(EntityIdJsonConverter<,>).MakeGenericType(typeToConvert,
                    stronglyTypedIdTypeInterface.GetGenericArguments()[0]);
                var v = Activator.CreateInstance(type);
                return v == null ? throw new InvalidOperationException($"Cannot create converter for '{typeToConvert}'") : (JsonConverter)v;
            }
            throw new InvalidOperationException($"Cannot create converter for '{typeToConvert}'");
        }
    }
}