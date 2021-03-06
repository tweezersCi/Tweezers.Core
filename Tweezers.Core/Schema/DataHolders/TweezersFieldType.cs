﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tweezers.Schema.DataHolders
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TweezersFieldType
    {
        Enum,
        Integer,
        String,
        Boolean,
        Array,
        Object,
        Password,
        TagsArray,
    }
}