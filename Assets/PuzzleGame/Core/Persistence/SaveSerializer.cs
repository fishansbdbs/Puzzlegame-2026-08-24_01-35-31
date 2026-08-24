using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace PuzzleGame.Core.Persistence
{
    public enum SaveDeserializationStatus
    {
        Loaded,
        Migrated,
        UnsupportedVersion
    }

    public sealed class SaveDeserializationResult
    {
        private readonly SaveData data;

        internal SaveDeserializationResult(SaveDeserializationStatus status, SaveData data, int sourceVersion)
        {
            Status = status;
            this.data = data == null ? null : SaveDataSnapshot.CreateValidated(data);
            SourceVersion = sourceVersion;
        }

        public SaveDeserializationStatus Status { get; private set; }
        public int SourceVersion { get; private set; }
        public SaveData Data { get { return data == null ? null : SaveDataSnapshot.CreateValidated(data); } }
    }

    public sealed class SaveSerializer
    {
        public const int MaximumInputCharacters = 1024 * 1024;
        public const int MaximumNestingDepth = 32;
        public const int MaximumCollectionElements = 10000;
        private const int MaximumObjectMembers = 256;

        public string Serialize(SaveData data)
        {
            var snapshot = SaveDataSnapshot.CreateValidated(data);
            var writer = new BoundedJsonWriter(MaximumInputCharacters);
            writer.WriteSave(snapshot);
            return writer.ToString();
        }

        public SaveDeserializationResult Deserialize(string json)
        {
            var root = JsonReader.Parse(json, MaximumInputCharacters, MaximumNestingDepth, MaximumCollectionElements, MaximumObjectMembers) as JsonObject;
            if (root == null) throw new FormatException("Save root must be a JSON object.");
            var version = ReadInt(root.GetRequired("Version"), "Version");
            if (version > SaveData.CurrentVersion)
                return new SaveDeserializationResult(SaveDeserializationStatus.UnsupportedVersion, null, version);
            if (version == 1)
                return new SaveDeserializationResult(SaveDeserializationStatus.Migrated, MapVersionOne(root), version);
            if (version == SaveData.CurrentVersion)
                return new SaveDeserializationResult(SaveDeserializationStatus.Loaded, MapVersionTwo(root), version);
            throw new ArgumentException("Save version must be a supported positive version.", "json");
        }

        private static SaveData MapVersionOne(JsonObject root)
        {
            var data = SaveData.CreateDefault();
            data.Wallet.Gold = ReadOptionalInt(root, "Gold", 0);
            data.Wallet.Gems = ReadOptionalInt(root, "Gems", 0);
            data.Characters = ReadCharacters(root, "Characters");
            data.PartyCharacterIds = ReadParty(root, "PartyCharacterIds");
            return SaveDataSnapshot.CreateValidated(data);
        }

        private static SaveData MapVersionTwo(JsonObject root)
        {
            var data = SaveData.CreateDefault();
            JsonValue value;
            if (root.TryGet("Wallet", out value)) data.Wallet = ReadWallet(AsObject(value, "Wallet"));
            data.Characters = ReadCharacters(root, "Characters");
            data.Materials = ReadMaterials(root, "Materials");
            data.PartyCharacterIds = ReadParty(root, "PartyCharacterIds");
            data.Banners = ReadBanners(root, "Banners");
            data.Stages = ReadStages(root, "Stages");
            if (root.TryGet("Settings", out value)) data.Settings = ReadSettings(AsObject(value, "Settings"));
            return SaveDataSnapshot.CreateValidated(data);
        }

        private static WalletSaveData ReadWallet(JsonObject value)
        {
            return new WalletSaveData
            {
                Gold = ReadOptionalInt(value, "Gold", 0),
                Gems = ReadOptionalInt(value, "Gems", 0),
                Tickets = ReadOptionalInt(value, "Tickets", 0),
                UniversalDuplicateResource = ReadOptionalInt(value, "UniversalDuplicateResource", 0),
                EventCurrencies = ReadCurrencyBalances(value, "EventCurrencies")
            };
        }

        private static CurrencyBalanceSaveData[] ReadCurrencyBalances(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return Array.Empty<CurrencyBalanceSaveData>();
            var array = AsArray(value, name);
            var result = new CurrencyBalanceSaveData[array.Values.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var item = AsObject(array.Values[index], name + " item");
                result[index] = new CurrencyBalanceSaveData
                {
                    CurrencyId = ReadRequiredString(item, "CurrencyId"),
                    Balance = ReadOptionalInt(item, "Balance", 0)
                };
            }
            return result;
        }

        private static CharacterProgressSaveData[] ReadCharacters(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return Array.Empty<CharacterProgressSaveData>();
            var array = AsArray(value, name);
            var result = new CharacterProgressSaveData[array.Values.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var item = AsObject(array.Values[index], name + " item");
                result[index] = new CharacterProgressSaveData
                {
                    CharacterId = ReadRequiredString(item, "CharacterId"),
                    TotalExperience = ReadOptionalInt(item, "TotalExperience", 0),
                    Ascension = ReadOptionalInt(item, "Ascension", 0),
                    Awakened = ReadOptionalBoolean(item, "Awakened", false)
                };
            }
            return result;
        }

        private static MaterialBalanceSaveData[] ReadMaterials(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return Array.Empty<MaterialBalanceSaveData>();
            var array = AsArray(value, name);
            var result = new MaterialBalanceSaveData[array.Values.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var item = AsObject(array.Values[index], name + " item");
                result[index] = new MaterialBalanceSaveData
                {
                    MaterialId = ReadRequiredString(item, "MaterialId"),
                    Balance = ReadOptionalInt(item, "Balance", 0)
                };
            }
            return result;
        }

        private static string[] ReadParty(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return new[] { "", "", "", "", "" };
            var array = AsArray(value, name);
            var result = new string[array.Values.Count];
            for (var index = 0; index < result.Length; index++)
            {
                if (array.Values[index] is JsonNull) result[index] = "";
                else result[index] = AsString(array.Values[index], name + " item").Value;
            }
            return result;
        }

        private static BannerRuntimeSaveData[] ReadBanners(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return Array.Empty<BannerRuntimeSaveData>();
            var array = AsArray(value, name);
            var result = new BannerRuntimeSaveData[array.Values.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var item = AsObject(array.Values[index], name + " item");
                result[index] = new BannerRuntimeSaveData
                {
                    BannerId = ReadRequiredString(item, "BannerId"),
                    RotationId = ReadRequiredString(item, "RotationId"),
                    AuthoredStepCount = ReadOptionalInt(item, "AuthoredStepCount", 0),
                    NextStepIndex = ReadOptionalInt(item, "NextStepIndex", 0)
                };
            }
            return result;
        }

        private static StageProgressSaveData[] ReadStages(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return Array.Empty<StageProgressSaveData>();
            var array = AsArray(value, name);
            var result = new StageProgressSaveData[array.Values.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var item = AsObject(array.Values[index], name + " item");
                result[index] = new StageProgressSaveData
                {
                    StageId = ReadRequiredString(item, "StageId"),
                    IsCleared = ReadOptionalBoolean(item, "IsCleared", false),
                    Stars = ReadStars(item, "Stars"),
                    BestFinalHpBasisPoints = ReadOptionalInt(item, "BestFinalHpBasisPoints", 0),
                    BestBoardResolutionCount = ReadOptionalInt(item, "BestBoardResolutionCount", 0)
                };
            }
            return result;
        }

        private static bool[] ReadStars(JsonObject parent, string name)
        {
            JsonValue value;
            if (!parent.TryGet(name, out value)) return new[] { false, false, false };
            var array = AsArray(value, name);
            var result = new bool[array.Values.Count];
            for (var index = 0; index < result.Length; index++) result[index] = AsBoolean(array.Values[index], name + " item").Value;
            return result;
        }

        private static SettingsSaveData ReadSettings(JsonObject value)
        {
            return new SettingsSaveData
            {
                MusicVolumePercent = ReadOptionalInt(value, "MusicVolumePercent", 100),
                EffectsVolumePercent = ReadOptionalInt(value, "EffectsVolumePercent", 100),
                ReducedMotion = ReadOptionalBoolean(value, "ReducedMotion", false),
                VibrationEnabled = ReadOptionalBoolean(value, "VibrationEnabled", true)
            };
        }

        private static string ReadRequiredString(JsonObject parent, string name)
        {
            return AsString(parent.GetRequired(name), name).Value;
        }

        private static int ReadOptionalInt(JsonObject parent, string name, int defaultValue)
        {
            JsonValue value;
            return parent.TryGet(name, out value) ? ReadInt(value, name) : defaultValue;
        }

        private static int ReadInt(JsonValue value, string name)
        {
            var number = value as JsonNumber;
            int result;
            if (number == null || !int.TryParse(number.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
                throw new FormatException(name + " must be an Int32 integer.");
            return result;
        }

        private static bool ReadOptionalBoolean(JsonObject parent, string name, bool defaultValue)
        {
            JsonValue value;
            return parent.TryGet(name, out value) ? AsBoolean(value, name).Value : defaultValue;
        }

        private static JsonObject AsObject(JsonValue value, string name)
        {
            var result = value as JsonObject;
            if (result == null) throw new FormatException(name + " must be an object.");
            return result;
        }

        private static JsonArray AsArray(JsonValue value, string name)
        {
            var result = value as JsonArray;
            if (result == null) throw new FormatException(name + " must be an array.");
            return result;
        }

        private static JsonString AsString(JsonValue value, string name)
        {
            var result = value as JsonString;
            if (result == null) throw new FormatException(name + " must be a string.");
            return result;
        }

        private static JsonBoolean AsBoolean(JsonValue value, string name)
        {
            var result = value as JsonBoolean;
            if (result == null) throw new FormatException(name + " must be a boolean.");
            return result;
        }

        private abstract class JsonValue { }

        private sealed class JsonObject : JsonValue
        {
            private readonly Dictionary<string, JsonValue> members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);

            internal int Count { get { return members.Count; } }

            internal void Add(string name, JsonValue value)
            {
                if (members.ContainsKey(name)) throw new FormatException("Duplicate JSON member: " + name);
                members.Add(name, value);
            }

            internal bool TryGet(string name, out JsonValue value)
            {
                return members.TryGetValue(name, out value);
            }

            internal JsonValue GetRequired(string name)
            {
                JsonValue value;
                if (!members.TryGetValue(name, out value)) throw new FormatException("Required JSON member is missing: " + name);
                return value;
            }
        }

        private sealed class JsonArray : JsonValue
        {
            internal readonly List<JsonValue> Values = new List<JsonValue>();
        }

        private sealed class JsonString : JsonValue
        {
            internal JsonString(string value) { Value = value; }
            internal string Value { get; private set; }
        }

        private sealed class JsonNumber : JsonValue
        {
            internal JsonNumber(string text) { Text = text; }
            internal string Text { get; private set; }
        }

        private sealed class JsonBoolean : JsonValue
        {
            internal JsonBoolean(bool value) { Value = value; }
            internal bool Value { get; private set; }
        }

        private sealed class JsonNull : JsonValue
        {
            internal static readonly JsonNull Value = new JsonNull();
            private JsonNull() { }
        }

        private sealed class JsonReader
        {
            private readonly string text;
            private readonly int maximumDepth;
            private readonly int maximumElements;
            private readonly int maximumMembers;
            private int position;

            private JsonReader(string text, int maximumDepth, int maximumElements, int maximumMembers)
            {
                this.text = text;
                this.maximumDepth = maximumDepth;
                this.maximumElements = maximumElements;
                this.maximumMembers = maximumMembers;
            }

            internal static JsonValue Parse(string text, int maximumCharacters, int maximumDepth, int maximumElements, int maximumMembers)
            {
                if (text == null) throw new ArgumentNullException("text");
                if (text.Length > maximumCharacters) throw new FormatException("Save JSON exceeds the input limit.");
                var reader = new JsonReader(text, maximumDepth, maximumElements, maximumMembers);
                reader.SkipWhitespace();
                if (reader.position == text.Length) throw new FormatException("Save JSON is empty.");
                var result = reader.ReadValue(1);
                reader.SkipWhitespace();
                if (reader.position != text.Length) throw new FormatException("Trailing content follows the save JSON.");
                return result;
            }

            private JsonValue ReadValue(int depth)
            {
                if (depth > maximumDepth) throw new FormatException("Save JSON exceeds the nesting limit.");
                if (position >= text.Length) throw new FormatException("Unexpected end of save JSON.");
                var current = text[position];
                if (current == '{') return ReadObject(depth);
                if (current == '[') return ReadArray(depth);
                if (current == '"') return new JsonString(ReadString());
                if (current == '-' || (current >= '0' && current <= '9')) return ReadNumber();
                if (TryReadLiteral("true")) return new JsonBoolean(true);
                if (TryReadLiteral("false")) return new JsonBoolean(false);
                if (TryReadLiteral("null")) return JsonNull.Value;
                throw new FormatException("Unexpected JSON token at character " + position + ".");
            }

            private JsonObject ReadObject(int depth)
            {
                position++;
                var result = new JsonObject();
                SkipWhitespace();
                if (TryRead('}')) return result;
                while (true)
                {
                    if (position >= text.Length || text[position] != '"') throw new FormatException("JSON object member name must be a string.");
                    var name = ReadString();
                    SkipWhitespace();
                    Require(':');
                    SkipWhitespace();
                    result.Add(name, ReadValue(depth + 1));
                    if (result.Count > maximumMembers) throw new FormatException("JSON object exceeds the member limit.");
                    SkipWhitespace();
                    if (TryRead('}')) return result;
                    Require(',');
                    SkipWhitespace();
                }
            }

            private JsonArray ReadArray(int depth)
            {
                position++;
                var result = new JsonArray();
                SkipWhitespace();
                if (TryRead(']')) return result;
                while (true)
                {
                    result.Values.Add(ReadValue(depth + 1));
                    if (result.Values.Count > maximumElements) throw new FormatException("JSON array exceeds the element limit.");
                    SkipWhitespace();
                    if (TryRead(']')) return result;
                    Require(',');
                    SkipWhitespace();
                }
            }

            private JsonNumber ReadNumber()
            {
                var start = position;
                if (text[position] == '-')
                {
                    position++;
                    if (position >= text.Length) throw new FormatException("JSON integer is incomplete.");
                }
                if (text[position] == '0')
                {
                    position++;
                    if (position < text.Length && text[position] >= '0' && text[position] <= '9')
                        throw new FormatException("JSON integers cannot contain leading zeroes.");
                }
                else
                {
                    if (text[position] < '1' || text[position] > '9') throw new FormatException("JSON integer is malformed.");
                    while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++;
                }
                if (position < text.Length && text[position] == '.')
                {
                    position++;
                    if (position >= text.Length || text[position] < '0' || text[position] > '9')
                        throw new FormatException("JSON fraction is incomplete.");
                    while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++;
                }
                if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
                {
                    position++;
                    if (position < text.Length && (text[position] == '+' || text[position] == '-')) position++;
                    if (position >= text.Length || text[position] < '0' || text[position] > '9')
                        throw new FormatException("JSON exponent is incomplete.");
                    while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++;
                }
                return new JsonNumber(text.Substring(start, position - start));
            }

            private string ReadString()
            {
                Require('"');
                var result = new StringBuilder();
                while (position < text.Length)
                {
                    var current = text[position++];
                    if (current == '"') return result.ToString();
                    if (current < 0x20) throw new FormatException("JSON strings cannot contain control characters.");
                    if (current == '\\')
                    {
                        if (position >= text.Length) throw new FormatException("JSON escape is incomplete.");
                        var escape = text[position++];
                        switch (escape)
                        {
                            case '"': result.Append('"'); break;
                            case '\\': result.Append('\\'); break;
                            case '/': result.Append('/'); break;
                            case 'b': result.Append('\b'); break;
                            case 'f': result.Append('\f'); break;
                            case 'n': result.Append('\n'); break;
                            case 'r': result.Append('\r'); break;
                            case 't': result.Append('\t'); break;
                            case 'u': AppendEscapedUnicode(result); break;
                            default: throw new FormatException("JSON string contains an invalid escape.");
                        }
                        continue;
                    }
                    AppendRawUnicode(result, current);
                }
                throw new FormatException("JSON string is unterminated.");
            }

            private void AppendEscapedUnicode(StringBuilder result)
            {
                var first = ReadHexCodeUnit();
                if (char.IsLowSurrogate(first)) throw new FormatException("JSON string contains an unpaired low surrogate.");
                if (!char.IsHighSurrogate(first)) { result.Append(first); return; }
                if (position + 1 >= text.Length || text[position] != '\\' || text[position + 1] != 'u')
                    throw new FormatException("JSON string contains an unpaired high surrogate.");
                position += 2;
                var second = ReadHexCodeUnit();
                if (!char.IsLowSurrogate(second)) throw new FormatException("JSON string contains an unpaired high surrogate.");
                result.Append(first);
                result.Append(second);
            }

            private char ReadHexCodeUnit()
            {
                if (position + 4 > text.Length) throw new FormatException("Unicode escape is incomplete.");
                var value = 0;
                for (var index = 0; index < 4; index++)
                {
                    var digit = HexValue(text[position++]);
                    if (digit < 0) throw new FormatException("Unicode escape contains a non-hexadecimal digit.");
                    value = value * 16 + digit;
                }
                return (char)value;
            }

            private void AppendRawUnicode(StringBuilder result, char first)
            {
                if (char.IsLowSurrogate(first)) throw new FormatException("JSON string contains an unpaired low surrogate.");
                result.Append(first);
                if (!char.IsHighSurrogate(first)) return;
                if (position >= text.Length || !char.IsLowSurrogate(text[position]))
                    throw new FormatException("JSON string contains an unpaired high surrogate.");
                result.Append(text[position++]);
            }

            private static int HexValue(char value)
            {
                if (value >= '0' && value <= '9') return value - '0';
                if (value >= 'a' && value <= 'f') return value - 'a' + 10;
                if (value >= 'A' && value <= 'F') return value - 'A' + 10;
                return -1;
            }

            private bool TryReadLiteral(string literal)
            {
                if (position + literal.Length > text.Length) return false;
                for (var index = 0; index < literal.Length; index++) if (text[position + index] != literal[index]) return false;
                position += literal.Length;
                return true;
            }

            private void SkipWhitespace()
            {
                while (position < text.Length)
                {
                    var value = text[position];
                    if (value != ' ' && value != '\t' && value != '\r' && value != '\n') return;
                    position++;
                }
            }

            private bool TryRead(char expected)
            {
                if (position >= text.Length || text[position] != expected) return false;
                position++;
                return true;
            }

            private void Require(char expected)
            {
                if (!TryRead(expected)) throw new FormatException("Expected '" + expected + "' at character " + position + ".");
            }
        }

        private sealed class BoundedJsonWriter
        {
            private readonly StringBuilder builder = new StringBuilder();
            private readonly int maximumCharacters;

            internal BoundedJsonWriter(int maximumCharacters)
            {
                this.maximumCharacters = maximumCharacters;
            }

            internal void WriteSave(SaveData data)
            {
                Append("{\"Version\":"); WriteInt(data.Version);
                Append(",\"Wallet\":{");
                Append("\"Gold\":"); WriteInt(data.Wallet.Gold);
                Append(",\"Gems\":"); WriteInt(data.Wallet.Gems);
                Append(",\"Tickets\":"); WriteInt(data.Wallet.Tickets);
                Append(",\"UniversalDuplicateResource\":"); WriteInt(data.Wallet.UniversalDuplicateResource);
                Append(",\"EventCurrencies\":[");
                for (var index = 0; index < data.Wallet.EventCurrencies.Length; index++)
                {
                    if (index > 0) Append(',');
                    var entry = data.Wallet.EventCurrencies[index];
                    Append("{\"CurrencyId\":"); WriteString(entry.CurrencyId);
                    Append(",\"Balance\":"); WriteInt(entry.Balance); Append('}');
                }
                Append("]}");

                Append(",\"Characters\":[");
                for (var index = 0; index < data.Characters.Length; index++)
                {
                    if (index > 0) Append(',');
                    var entry = data.Characters[index];
                    Append("{\"CharacterId\":"); WriteString(entry.CharacterId);
                    Append(",\"TotalExperience\":"); WriteInt(entry.TotalExperience);
                    Append(",\"Ascension\":"); WriteInt(entry.Ascension);
                    Append(",\"Awakened\":"); WriteBoolean(entry.Awakened); Append('}');
                }
                Append(']');

                Append(",\"Materials\":[");
                for (var index = 0; index < data.Materials.Length; index++)
                {
                    if (index > 0) Append(',');
                    var entry = data.Materials[index];
                    Append("{\"MaterialId\":"); WriteString(entry.MaterialId);
                    Append(",\"Balance\":"); WriteInt(entry.Balance); Append('}');
                }
                Append(']');

                Append(",\"PartyCharacterIds\":[");
                for (var index = 0; index < data.PartyCharacterIds.Length; index++)
                {
                    if (index > 0) Append(',');
                    WriteString(data.PartyCharacterIds[index]);
                }
                Append(']');

                Append(",\"Banners\":[");
                for (var index = 0; index < data.Banners.Length; index++)
                {
                    if (index > 0) Append(',');
                    var entry = data.Banners[index];
                    Append("{\"BannerId\":"); WriteString(entry.BannerId);
                    Append(",\"RotationId\":"); WriteString(entry.RotationId);
                    Append(",\"AuthoredStepCount\":"); WriteInt(entry.AuthoredStepCount);
                    Append(",\"NextStepIndex\":"); WriteInt(entry.NextStepIndex); Append('}');
                }
                Append(']');

                Append(",\"Stages\":[");
                for (var index = 0; index < data.Stages.Length; index++)
                {
                    if (index > 0) Append(',');
                    var entry = data.Stages[index];
                    Append("{\"StageId\":"); WriteString(entry.StageId);
                    Append(",\"IsCleared\":"); WriteBoolean(entry.IsCleared);
                    Append(",\"Stars\":[");
                    for (var star = 0; star < entry.Stars.Length; star++)
                    {
                        if (star > 0) Append(',');
                        WriteBoolean(entry.Stars[star]);
                    }
                    Append("],\"BestFinalHpBasisPoints\":"); WriteInt(entry.BestFinalHpBasisPoints);
                    Append(",\"BestBoardResolutionCount\":"); WriteInt(entry.BestBoardResolutionCount); Append('}');
                }
                Append(']');

                Append(",\"Settings\":{");
                Append("\"MusicVolumePercent\":"); WriteInt(data.Settings.MusicVolumePercent);
                Append(",\"EffectsVolumePercent\":"); WriteInt(data.Settings.EffectsVolumePercent);
                Append(",\"ReducedMotion\":"); WriteBoolean(data.Settings.ReducedMotion);
                Append(",\"VibrationEnabled\":"); WriteBoolean(data.Settings.VibrationEnabled);
                Append("}}");
            }

            public override string ToString()
            {
                return builder.ToString();
            }

            private void WriteInt(int value)
            {
                Append(value.ToString(CultureInfo.InvariantCulture));
            }

            private void WriteBoolean(bool value)
            {
                Append(value ? "true" : "false");
            }

            private void WriteString(string value)
            {
                if (value == null) value = "";
                Append('"');
                for (var index = 0; index < value.Length; index++)
                {
                    var current = value[index];
                    switch (current)
                    {
                        case '"': Append("\\\""); break;
                        case '\\': Append("\\\\"); break;
                        case '\b': Append("\\b"); break;
                        case '\f': Append("\\f"); break;
                        case '\n': Append("\\n"); break;
                        case '\r': Append("\\r"); break;
                        case '\t': Append("\\t"); break;
                        default:
                            if (current < 0x20) { Append("\\u"); Append(((int)current).ToString("X4", CultureInfo.InvariantCulture)); }
                            else if (char.IsLowSurrogate(current)) throw new FormatException("Save strings cannot contain unpaired low surrogates.");
                            else if (char.IsHighSurrogate(current))
                            {
                                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                                    throw new FormatException("Save strings cannot contain unpaired high surrogates.");
                                Append(current); Append(value[++index]);
                            }
                            else Append(current);
                            break;
                    }
                }
                Append('"');
            }

            private void Append(char value)
            {
                if (builder.Length >= maximumCharacters) throw new FormatException("Serialized save exceeds the output limit.");
                builder.Append(value);
            }

            private void Append(string value)
            {
                if (value == null) return;
                if (value.Length > maximumCharacters - builder.Length) throw new FormatException("Serialized save exceeds the output limit.");
                builder.Append(value);
            }
        }
    }
}
