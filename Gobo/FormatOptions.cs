﻿using System.Text.Json.Serialization;

namespace Gobo;

public enum BraceStyle
{
    SameLine,
    NewLine,
}

public class FormatOptions
{
    public bool UseTabs { get; set; } = true;
    public int TabWidth { get; set; } = 4;
    public int Width { get; set; } = 300;

    [JsonIgnore]
    public BraceStyle BraceStyle { get; set; } = BraceStyle.NewLine;

    [JsonIgnore]
    public bool ValidateOutput { get; set; } = true;

    [JsonIgnore]
    public bool RemoveSyntaxExtensions { get; set; } = false;

    [JsonIgnore]
    public bool GetDebugInfo { get; set; } = true; // changed

    public static FormatOptions DefaultTestOptions { get; } = new() { GetDebugInfo = true };

    public static FormatOptions Default { get; } = new();
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(FormatOptions))]
public partial class FormatOptionsSerializer : JsonSerializerContext { }
