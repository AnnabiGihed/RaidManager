using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace RaidManager.Companion.Client.Features.Sync.SavedVariables;

/// <summary>Reads the Lua that World of Warcraft writes for SavedVariables into JSON, without running it.</summary>
/// <remarks>
/// Author: Gihed Annabi<br/>
/// Date: 2026-10-07<br/>
/// Purpose: The companion must never execute an addon file (#550). WoW writes only assignments of tables, strings,
/// numbers and booleans, so this parser reads exactly that subset: <c>name = value</c> statements, tables with
/// <c>[key] = value</c>, <c>key = value</c> and positional entries, quoted strings with escapes, numbers, booleans,
/// <c>nil</c> and comments. A table with positional entries only (or none) becomes a JSON array, any other an object.
/// A file that ends early keeps what was read and records the keys of the entries it cut, so the companion can upload
/// the characters written before the cut.
/// </remarks>
internal sealed class LuaTableParser
{
    #region Fields
    /// <summary>Stores the text being read.</summary>
    private readonly string _text;

    /// <summary>Stores the keys from the outermost cut entry to the innermost, filled while unwinding.</summary>
    private readonly List<string> _cutPath = [];

    /// <summary>Stores the read position.</summary>
    private int _position;

    /// <summary>Stores whether the text ended before the value was complete.</summary>
    private bool _truncated;
    #endregion Fields

    #region Constructors
    /// <summary>Initializes a new instance of the <see cref="LuaTableParser"/> class.</summary>
    /// <param name="text">The file's text.</param>
    private LuaTableParser(string text)
    {
        _text = text;
    }
    #endregion Constructors

    #region Private Properties
    /// <summary>Gets a value indicating whether the whole text was read.</summary>
    private bool AtEnd => _position >= _text.Length;
    #endregion Private Properties

    #region Public Methods
    /// <summary>Reads every top-level assignment of a SavedVariables file.</summary>
    /// <param name="text">The file's text.</param>
    /// <returns>The assigned values by global name, and where the file was cut if it ended early.</returns>
    /// <exception cref="FormatException">Thrown when the text isn't SavedVariables Lua.</exception>
    public static LuaDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parser = new LuaTableParser(text);
        var globals = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        parser.SkipTrivia();
        while (!parser.AtEnd && !parser._truncated)
        {
            var name = parser.ReadName();
            parser.SkipTrivia();
            if (parser.AtEnd)
            {
                parser._truncated = true;
                break;
            }

            parser.Expect('=');
            var value = parser.ReadValue();
            globals[name] = value;
            if (parser._truncated)
            {
                parser._cutPath.Insert(0, name);
            }

            parser.SkipTrivia();
        }

        return new LuaDocument(globals, parser._truncated, parser._cutPath);
    }
    #endregion Public Methods

    #region Private Helpers
    /// <summary>Determines whether a character can start a Lua name.</summary>
    /// <param name="character">The character.</param>
    /// <returns><see langword="true"/> for a letter or an underscore.</returns>
    private static bool IsNameStart(char character) => char.IsAsciiLetter(character) || character == '_';

    /// <summary>Builds a JSON object from keyed entries, leaving out <c>nil</c> values.</summary>
    /// <param name="entries">The entries.</param>
    /// <returns>The object.</returns>
    private static JsonObject ToObject(List<KeyValuePair<string, JsonNode?>> entries)
    {
        var result = new JsonObject();
        foreach (var (key, value) in entries)
        {
            if (value is not null)
            {
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>Builds a JSON array from positional entries.</summary>
    /// <param name="values">The values.</param>
    /// <returns>The array.</returns>
    private static JsonArray ToArray(List<JsonNode?> values) => new([.. values]);

    /// <summary>Reads one value; at the end of the text, marks the document truncated and returns <see langword="null"/>.</summary>
    /// <returns>The value as JSON, or <see langword="null"/> for <c>nil</c> or a cut value.</returns>
    private JsonNode? ReadValue()
    {
        SkipTrivia();
        if (AtEnd)
        {
            _truncated = true;
            return null;
        }

        var current = _text[_position];
        if (current == '{')
        {
            _position++;
            return ReadTable();
        }

        if (current is '"' or '\'')
        {
            var text = ReadString();
            return text is null ? null : JsonValue.Create(text);
        }

        if (current == '-' || current == '.' || char.IsAsciiDigit(current))
        {
            return ReadNumber();
        }

        if (IsNameStart(current))
        {
            return ReadKeyword();
        }

        throw Unexpected();
    }

    /// <summary>Reads a table's entries after its opening brace.</summary>
    /// <returns>A JSON array for positional entries only, otherwise a JSON object; a cut table returns what it read.</returns>
    private JsonNode ReadTable()
    {
        var entries = new List<KeyValuePair<string, JsonNode?>>();
        var positional = new List<JsonNode?>();
        var keyed = false;
        while (true)
        {
            SkipTrivia();
            if (AtEnd)
            {
                _truncated = true;
                break;
            }

            if (_text[_position] == '}')
            {
                _position++;
                break;
            }

            var key = ReadEntryKey();
            if (_truncated)
            {
                break;
            }

            var value = ReadValue();
            if (key is null)
            {
                positional.Add(value);
                entries.Add(new(positional.Count.ToString(CultureInfo.InvariantCulture), value));
            }
            else
            {
                keyed = true;
                entries.Add(new(key, value));
            }

            if (_truncated)
            {
                _cutPath.Insert(0, entries[^1].Key);
                break;
            }

            ReadSeparator();
        }

        return keyed ? ToObject(entries) : ToArray(positional);
    }

    /// <summary>Reads an entry's key and its equals sign, or nothing for a positional entry.</summary>
    /// <returns>The key, or <see langword="null"/> for a positional entry.</returns>
    private string? ReadEntryKey()
    {
        var start = _position;
        if (_text[_position] == '[' && !IsLongBracket())
        {
            _position++;
            var key = ReadValue();
            if (_truncated)
            {
                return null;
            }

            SkipTrivia();
            Expect(']');
            SkipTrivia();
            Expect('=');
            return key switch
            {
                JsonValue value when value.TryGetValue(out string? text) => text,
                JsonValue value => value.ToJsonString(),
                _ => throw new FormatException($"Unsupported table key at position {start}."),
            };
        }

        if (IsNameStart(_text[_position]))
        {
            var name = ReadName();
            SkipTrivia();
            if (!AtEnd && _text[_position] == '=' && !Peek(1, '='))
            {
                _position++;
                return name;
            }

            // A positional keyword such as true; read it again as a value.
            _position = start;
        }

        return null;
    }

    /// <summary>Reads the separator after an entry, if any.</summary>
    private void ReadSeparator()
    {
        SkipTrivia();
        if (AtEnd)
        {
            _truncated = true;
            return;
        }

        if (_text[_position] is ',' or ';')
        {
            _position++;
        }
        else if (_text[_position] != '}')
        {
            throw Unexpected();
        }
    }

    /// <summary>Reads a quoted string with Lua escapes.</summary>
    /// <returns>The string, or <see langword="null"/> when the text ends inside it.</returns>
    private string? ReadString()
    {
        var quote = _text[_position++];
        var builder = new StringBuilder();
        while (!AtEnd)
        {
            var current = _text[_position++];
            if (current == quote)
            {
                return builder.ToString();
            }

            if (current == '\\')
            {
                if (AtEnd)
                {
                    break;
                }

                ReadEscape(builder);
            }
            else if (current is '\n' or '\r')
            {
                throw new FormatException($"Unfinished string at position {_position}.");
            }
            else
            {
                builder.Append(current);
            }
        }

        _truncated = true;
        return null;
    }

    /// <summary>Reads one escape sequence after its backslash.</summary>
    /// <param name="builder">The string being built.</param>
    private void ReadEscape(StringBuilder builder)
    {
        var escaped = _text[_position++];
        switch (escaped)
        {
            case 'n':
            case '\n':
                builder.Append('\n');
                break;
            case 'r':
                builder.Append('\r');
                break;
            case 't':
                builder.Append('\t');
                break;
            case 'a':
                builder.Append('\a');
                break;
            case 'b':
                builder.Append('\b');
                break;
            case 'f':
                builder.Append('\f');
                break;
            case 'v':
                builder.Append('\v');
                break;
            case '\\' or '"' or '\'':
                builder.Append(escaped);
                break;
            default:
                if (!char.IsAsciiDigit(escaped))
                {
                    throw new FormatException($"Unknown escape at position {_position}.");
                }

                // Lua writes a byte as up to three decimal digits; WoW strings are UTF-8 bytes.
                var digits = escaped.ToString();
                while (digits.Length < 3 && !AtEnd && char.IsAsciiDigit(_text[_position]))
                {
                    digits += _text[_position++];
                }

                builder.Append((char)int.Parse(digits, CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <summary>Reads a number: an integer when it has no fraction or exponent and fits, otherwise a double.</summary>
    /// <remarks>A number cut by the end of the text reads as a shorter one, but the table around it then lacks its
    /// closing brace, which marks the cut.</remarks>
    /// <returns>The number.</returns>
    private JsonValue ReadNumber()
    {
        var start = _position;
        if (_text[_position] == '-')
        {
            _position++;
        }

        while (!AtEnd && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] is '.' || IsExponentSign()))
        {
            _position++;
        }

        var token = _text[start.._position];
        if (long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var real) && double.IsFinite(real))
        {
            return JsonValue.Create(real);
        }

        throw new FormatException($"Unsupported number '{token}' at position {start}.");
    }

    /// <summary>Reads <c>true</c>, <c>false</c> or <c>nil</c>.</summary>
    /// <returns>The boolean, or <see langword="null"/> for <c>nil</c> or a cut keyword.</returns>
    private JsonValue? ReadKeyword()
    {
        var start = _position;
        var name = ReadName();
        switch (name)
        {
            case "true":
                return JsonValue.Create(true);
            case "false":
                return JsonValue.Create(false);
            case "nil":
                return null;
            default:
                // A keyword the end of the text cut, such as "tru", marks the file cut rather than wrong.
                _truncated = AtEnd ? true : throw new FormatException($"Unexpected name '{name}' at position {start}.");
                return null;
        }
    }

    /// <summary>Reads a Lua name.</summary>
    /// <returns>The name.</returns>
    private string ReadName()
    {
        var start = _position;
        if (AtEnd || !IsNameStart(_text[_position]))
        {
            throw Unexpected();
        }

        while (!AtEnd && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] == '_'))
        {
            _position++;
        }

        return _text[start.._position];
    }

    /// <summary>Skips white space and comments; a comment cut by the end of the text marks it truncated.</summary>
    private void SkipTrivia()
    {
        while (!AtEnd)
        {
            if (char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
            else if (_text[_position] == '-' && Peek(1, '-'))
            {
                SkipComment();
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>Skips a line comment or a long comment.</summary>
    private void SkipComment()
    {
        _position += 2;
        if (IsLongBracket())
        {
            var end = _text.IndexOf("]]", _position, StringComparison.Ordinal);
            if (end < 0)
            {
                _truncated = true;
                _position = _text.Length;
                return;
            }

            _position = end + 2;
            return;
        }

        var lineEnd = _text.IndexOf('\n', _position);
        _position = lineEnd < 0 ? _text.Length : lineEnd + 1;
    }

    /// <summary>Consumes an expected character.</summary>
    /// <param name="expected">The character.</param>
    private void Expect(char expected)
    {
        if (AtEnd)
        {
            _truncated = true;
            return;
        }

        if (_text[_position] != expected)
        {
            throw Unexpected();
        }

        _position++;
    }

    /// <summary>Determines whether a character follows at an offset.</summary>
    /// <param name="offset">The offset from the read position.</param>
    /// <param name="expected">The character.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private bool Peek(int offset, char expected) => _position + offset < _text.Length && _text[_position + offset] == expected;

    /// <summary>Determines whether the read position starts a long bracket, <c>[[</c>.</summary>
    /// <returns><see langword="true"/> when it does.</returns>
    private bool IsLongBracket() => !AtEnd && _text[_position] == '[' && Peek(1, '[');

    /// <summary>Determines whether the read position is a sign inside an exponent, such as the minus of <c>1e-5</c>.</summary>
    /// <returns><see langword="true"/> when it is.</returns>
    private bool IsExponentSign() =>
        _text[_position] is '+' or '-' && _position > 0 && _text[_position - 1] is 'e' or 'E';

    /// <summary>Builds the failure for an unexpected character.</summary>
    /// <returns>The failure.</returns>
    private FormatException Unexpected() =>
        new($"Unexpected '{(AtEnd ? "end" : _text[_position].ToString())}' at position {_position}.");

    #endregion Private Helpers
}
