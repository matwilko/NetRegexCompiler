// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This RegexParser class is internal to the Regex package.
// It builds a tree of RegexNodes from a regular expression

// Implementation notes:
//
// It would be nice to get rid of the comment modes, since the
// ScanBlank() calls are just kind of duct-taped in.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    internal sealed class RegexParser
    {
        private const int EscapeMaxBufferSize = 256;
        private const int OptionStackDefaultSize = 32;
        private const int MaxValueDiv10 = int.MaxValue / 10;
        private const int MaxValueMod10 = int.MaxValue % 10;

        private RegexNode _stack;
        private RegexNode _group;
        private RegexNode _alternation;
        private RegexNode _concatenation;
        private RegexNode _unit;

        private readonly string _pattern;
        private int _currentPos;
        private readonly CultureInfo _culture;

        private int _autocap;
        private int _capcount;
        private int _captop;
        private int _capsize;

        private Dictionary<int, int> _caps;
        private Dictionary<string, int> _capnames;

        private int[] _capnumlist;
        private List<string> _capnamelist;

        private RegexOptions _options;
        private Stack<RegexOptions> _optionsStack;

        private bool _ignoreNextParen; // flag to skip capturing a parentheses group

        private RegexParser(string pattern, RegexOptions options, CultureInfo culture, Dictionary<int, int> caps, int capsize, Dictionary<string, int> capnames)
        {
            Debug.Assert(pattern != null, "Pattern must be set");
            Debug.Assert(culture != null, "Culture must be set");

            _pattern = pattern;
            _options = options;
            _culture = culture;
            _caps = caps;
            _capsize = capsize;
            _capnames = capnames;

            _optionsStack = new Stack<RegexOptions>();
            _stack = default;
            _group = default;
            _alternation = default;
            _concatenation = default;
            _unit = default;
            _currentPos = 0;
            _autocap = default;
            _capcount = default;
            _captop = default;
            _capnumlist = default;
            _capnamelist = default;
            _ignoreNextParen = false;
        }

        private RegexParser(string pattern, RegexOptions options, CultureInfo culture)
            : this(pattern, options, culture, new Dictionary<int, int>(), default, null)
        {
        }

        public static RegexTree Parse(string pattern, RegexOptions options, CultureInfo culture)
        {
            var parser = new RegexParser(pattern, options, culture);

            parser.CountCaptures();
            parser.Reset(options);
            RegexNode root = parser.ScanRegex();
            string[] capnamelist = parser._capnamelist?.ToArray();
            var tree = new RegexTree(root, parser._caps, parser._capnumlist, parser._captop, parser._capnames, capnamelist, options);

            return tree;
        }

        /// <summary>Escapes all metacharacters (including |,(,),[,{,|,^,$,*,+,?,\, spaces and #)</summary>
        public static string Escape(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (IsMetachar(input[i]))
                {
                    return EscapeImpl(input, i);
                }
            }

            return input;
        }

        private static string EscapeImpl(string input, int i)
        {
            var vsb = new StringBuilder(input.Length + 200);

            char ch = input[i];
            vsb.Append(input, 0, i);

            do
            {
                vsb.Append('\\');
                switch (ch)
                {
                    case '\n':
                        ch = 'n';
                        break;
                    case '\r':
                        ch = 'r';
                        break;
                    case '\t':
                        ch = 't';
                        break;
                    case '\f':
                        ch = 'f';
                        break;
                }
                vsb.Append(ch);
                i++;
                int lastpos = i;

                while (i < input.Length)
                {
                    ch = input[i];
                    if (IsMetachar(ch))
                        break;

                    i++;
                }

                vsb.Append(input, lastpos, i - lastpos);
            } while (i < input.Length);

            return vsb.ToString();
        }
        
        /// <summary>Resets parsing to the beginning of the pattern.</summary>
        private void Reset(RegexOptions options)
        {
            _currentPos = 0;
            _autocap = 1;
            _ignoreNextParen = false;
            _optionsStack.Clear();
            _options = options;
            _stack = null;
        }
        
        private RegexNode ScanRegex()
        {
            char ch = '@'; // nonspecial ch, means at beginning
            bool isQuantifier = false;

            StartGroup(new RegexNode(RegexNode.Capture, _options, 0, -1));

            while (CharsRight > 0)
            {
                bool wasPrevQuantifier = isQuantifier;
                isQuantifier = false;

                ScanBlank();

                int startpos = Textpos;

                // move past all of the normal characters.  We'll stop when we hit some kind of control character,
                // or if IgnorePatternWhiteSpace is on, we'll stop when we see some whitespace.
                if (IgnorePatternWhitespace)
                    while (CharsRight > 0 && (!IsStopperX(ch = RightChar()) || (ch == '{' && !IsTrueQuantifier())))
                        MoveRight();
                else
                    while (CharsRight > 0 && (!IsSpecial(ch = RightChar()) || (ch == '{' && !IsTrueQuantifier())))
                        MoveRight();

                int endpos = Textpos;

                ScanBlank();

                if (CharsRight == 0)
                    ch = '!'; // nonspecial, means at end
                else if (IsSpecial(ch = RightChar()))
                {
                    isQuantifier = IsQuantifier(ch);
                    MoveRight();
                }
                else
                    ch = ' '; // nonspecial, means at ordinary char

                if (startpos < endpos)
                {
                    int cchUnquantified = endpos - startpos - (isQuantifier ? 1 : 0);

                    wasPrevQuantifier = false;

                    if (cchUnquantified > 0)
                        AddConcatenate(startpos, cchUnquantified, false);

                    if (isQuantifier)
                        AddUnitOne(CharAt(endpos - 1));
                }

                switch (ch)
                {
                    case '!':
                        goto BreakOuterScan;

                    case ' ':
                        goto ContinueOuterScan;

                    case '[':
                        AddUnitSet(ScanCharClass(IsCaseInsensitive, scanOnly: false).ToStringClass());
                        break;

                    case '(':
                        {
                            RegexNode grouper;

                            PushOptions();

                            if (null == (grouper = ScanGroupOpen()))
                            {
                                PopKeepOptions();
                            }
                            else
                            {
                                PushGroup();
                                StartGroup(grouper);
                            }
                        }
                        continue;

                    case '|':
                        AddAlternate();
                        goto ContinueOuterScan;

                    case ')':
                        if (EmptyStack())
                            throw MakeException(RegexParseError.TooManyParentheses, "Too many )'s.");

                        AddGroup();
                        PopGroup();
                        PopOptions();

                        if (Unit() == null)
                            goto ContinueOuterScan;
                        break;

                    case '\\':
                        if (CharsRight == 0)
                            throw MakeException(RegexParseError.IllegalEndEscape, "Illegal \\ at end of pattern.");

                        AddUnitNode(ScanBackslash(scanOnly: false));
                        break;

                    case '^':
                        AddUnitType(IsMultiLine ? RegexNode.Bol : RegexNode.Beginning);
                        break;

                    case '$':
                        AddUnitType(IsMultiLine ? RegexNode.Eol : RegexNode.EndZ);
                        break;

                    case '.':
                        if (IsSingleLine)
                            AddUnitSet(RegexCharClass.AnyClass);
                        else
                            AddUnitNotone('\n');
                        break;

                    case '{':
                    case '*':
                    case '+':
                    case '?':
                        if (Unit() == null)
                        {
                            if (wasPrevQuantifier)
                                throw MakeException(RegexParseError.NestedQuantify, $"Nested quantifier '{ch}'.");
                            else
                                throw MakeException(RegexParseError.QuantifyAfterNothing, "Quantifier {x,y} following nothing.");
                        }
                        MoveLeft();
                        break;

                    default:
                        throw new InvalidOperationException("Internal error in ScanRegex.");
                }

                ScanBlank();

                if (CharsRight == 0 || !(isQuantifier = IsTrueQuantifier()))
                {
                    AddConcatenate();
                    goto ContinueOuterScan;
                }

                ch = RightCharMoveRight();

                // Handle quantifiers
                while (Unit() != null)
                {
                    int min;
                    int max;
                    bool lazy;

                    switch (ch)
                    {
                        case '*':
                            min = 0;
                            max = int.MaxValue;
                            break;

                        case '?':
                            min = 0;
                            max = 1;
                            break;

                        case '+':
                            min = 1;
                            max = int.MaxValue;
                            break;

                        case '{':
                            {
                                startpos = Textpos;
                                max = min = ScanDecimal();
                                if (startpos < Textpos)
                                {
                                    if (CharsRight > 0 && RightChar() == ',')
                                    {
                                        MoveRight();
                                        if (CharsRight == 0 || RightChar() == '}')
                                            max = int.MaxValue;
                                        else
                                            max = ScanDecimal();
                                    }
                                }

                                if (startpos == Textpos || CharsRight == 0 || RightCharMoveRight() != '}')
                                {
                                    AddConcatenate();
                                    Textto(startpos - 1);
                                    goto ContinueOuterScan;
                                }
                            }

                            break;

                        default:
                            throw new InvalidOperationException("Internal error in ScanRegex.");
                    }

                    ScanBlank();

                    if (CharsRight == 0 || RightChar() != '?')
                        lazy = false;
                    else
                    {
                        MoveRight();
                        lazy = true;
                    }

                    if (min > max)
                        throw MakeException(RegexParseError.IllegalRange, "Illegal {x,y} with x > y.");

                    AddConcatenate(lazy, min, max);
                }

            ContinueOuterScan:
                ;
            }

        BreakOuterScan:
            ;

            if (!EmptyStack())
                throw MakeException(RegexParseError.NotEnoughParentheses, "Not enough )'s.");

            AddGroup();

            return Unit();
        }

        /// <summary>Scans contents of [] (not including []'s), and converts to a RegexCharClass.</summary> 
        private RegexCharClass ScanCharClass(bool caseInsensitive, bool scanOnly)
        {
            char ch = '\0';
            char chPrev = '\0';
            bool inRange = false;
            bool firstChar = true;
            bool closed = false;

            RegexCharClass cc;

            cc = scanOnly ? null : new RegexCharClass();

            if (CharsRight > 0 && RightChar() == '^')
            {
                MoveRight();
                if (!scanOnly)
                    cc.Negate = true;
            }

            for (; CharsRight > 0; firstChar = false)
            {
                bool fTranslatedChar = false;
                ch = RightCharMoveRight();
                if (ch == ']')
                {
                    if (!firstChar)
                    {
                        closed = true;
                        break;
                    }
                }
                else if (ch == '\\' && CharsRight > 0)
                {
                    switch (ch = RightCharMoveRight())
                    {
                        case 'D':
                        case 'd':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, $"Cannot include class \\{ch} in character range.");
                                cc.AddDigit(IsEcmaScript, ch == 'D', _pattern, _currentPos);
                            }
                            continue;

                        case 'S':
                        case 's':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, $"Cannot include class \\{ch} in character range.");
                                cc.AddSpace(IsEcmaScript, ch == 'S');
                            }
                            continue;

                        case 'W':
                        case 'w':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, $"Cannot include class \\{ch} in character range.");

                                cc.AddWord(IsEcmaScript, ch == 'W');
                            }
                            continue;

                        case 'p':
                        case 'P':
                            if (!scanOnly)
                            {
                                if (inRange)
                                    throw MakeException(RegexParseError.BadClassInCharRange, $"Cannot include class \\{ch} in character range.");
                                cc.AddCategoryFromName(ParseProperty(), (ch != 'p'), caseInsensitive, _pattern, _currentPos);
                            }
                            else
                                ParseProperty();

                            continue;

                        case '-':
                            if (!scanOnly)
                                cc.AddRange(ch, ch);
                            continue;

                        default:
                            MoveLeft();
                            ch = ScanCharEscape(); // non-literal character
                            fTranslatedChar = true;
                            break;          // this break will only break out of the switch
                    }
                }
                else if (ch == '[')
                {
                    // This is code for Posix style properties - [:Ll:] or [:IsTibetan:].
                    // It currently doesn't do anything other than skip the whole thing!
                    if (CharsRight > 0 && RightChar() == ':' && !inRange)
                    {
                        int savePos = Textpos;

                        MoveRight();
                        if (CharsRight < 2 || RightCharMoveRight() != ':' || RightCharMoveRight() != ']')
                            Textto(savePos);
                    }
                }

                if (inRange)
                {
                    inRange = false;
                    if (!scanOnly)
                    {
                        if (ch == '[' && !fTranslatedChar && !firstChar)
                        {
                            // We thought we were in a range, but we're actually starting a subtraction.
                            // In that case, we'll add chPrev to our char class, skip the opening [, and
                            // scan the new character class recursively.
                            cc.AddChar(chPrev);
                            cc.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));

                            if (CharsRight > 0 && RightChar() != ']')
                                throw MakeException(RegexParseError.SubtractionMustBeLast, "A subtraction must be the last element in a character class.");
                        }
                        else
                        {
                            // a regular range, like a-z
                            if (chPrev > ch)
                                throw MakeException(RegexParseError.ReversedCharRange, "[x-y] range in reverse order.");
                            cc.AddRange(chPrev, ch);
                        }
                    }
                }
                else if (CharsRight >= 2 && RightChar() == '-' && RightChar(1) != ']')
                {
                    // this could be the start of a range
                    chPrev = ch;
                    inRange = true;
                    MoveRight();
                }
                else if (CharsRight >= 1 && ch == '-' && !fTranslatedChar && RightChar() == '[' && !firstChar)
                {
                    // we aren't in a range, and now there is a subtraction.  Usually this happens
                    // only when a subtraction follows a range, like [a-z-[b]]
                    if (!scanOnly)
                    {
                        MoveRight(1);
                        cc.AddSubtraction(ScanCharClass(caseInsensitive, scanOnly));

                        if (CharsRight > 0 && RightChar() != ']')
                            throw MakeException(RegexParseError.SubtractionMustBeLast, "A subtraction must be the last element in a character class.");
                    }
                    else
                    {
                        MoveRight(1);
                        ScanCharClass(caseInsensitive, scanOnly);
                    }
                }
                else
                {
                    if (!scanOnly)
                        cc.AddRange(ch, ch);
                }
            }

            if (!closed)
                throw MakeException(RegexParseError.UnterminatedBracket, "Unterminated [] set.");

            if (!scanOnly && caseInsensitive)
                cc.AddLowercase(_culture);

            return cc;
        }

        /// <summary>
        ///  Scans chars following a '(' (not counting the '('), and returns
        ///  a RegexNode for the type of group scanned, or null if the group
        ///  simply changed options (?cimsx-cimsx) or was a comment (#...).      
        /// </summary>
        private RegexNode ScanGroupOpen()
        {
            // just return a RegexNode if we have:
            // 1. "(" followed by nothing
            // 2. "(x" where x != ?
            // 3. "(?)"
            if (CharsRight == 0 || RightChar() != '?' || (RightChar() == '?' && (CharsRight > 1 && RightChar(1) == ')')))
            {
                if (IsExplicitCapture || _ignoreNextParen)
                {
                    _ignoreNextParen = false;
                    return new RegexNode(RegexNode.Group, _options);
                }
                else
                    return new RegexNode(RegexNode.Capture, _options, _autocap++, -1);
            }

            MoveRight();

            for (; ;)
            {
                if (CharsRight == 0)
                    break;

                int NodeType;
                char close = '>';
                char ch;
                switch (ch = RightCharMoveRight())
                {
                    case ':':
                        // noncapturing group
                        NodeType = RegexNode.Group;
                        break;

                    case '=':
                        // lookahead assertion
                        _options &= ~(RegexOptions.RightToLeft);
                        NodeType = RegexNode.Require;
                        break;

                    case '!':
                        // negative lookahead assertion
                        _options &= ~(RegexOptions.RightToLeft);
                        NodeType = RegexNode.Prevent;
                        break;

                    case '>':
                        // greedy subexpression
                        NodeType = RegexNode.Greedy;
                        break;

                    case '\'':
                        close = '\'';
                        goto case '<';
                    // fallthrough

                    case '<':
                        if (CharsRight == 0)
                            goto BreakRecognize;

                        switch (ch = RightCharMoveRight())
                        {
                            case '=':
                                if (close == '\'')
                                    goto BreakRecognize;

                                // lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                NodeType = RegexNode.Require;
                                break;

                            case '!':
                                if (close == '\'')
                                    goto BreakRecognize;

                                // negative lookbehind assertion
                                _options |= RegexOptions.RightToLeft;
                                NodeType = RegexNode.Prevent;
                                break;

                            default:
                                MoveLeft();
                                int capnum = -1;
                                int uncapnum = -1;
                                bool proceed = false;

                                // grab part before -

                                if (ch >= '0' && ch <= '9')
                                {
                                    capnum = ScanDecimal();

                                    if (!IsCaptureSlot(capnum))
                                        capnum = -1;

                                    // check if we have bogus characters after the number
                                    if (CharsRight > 0 && !(RightChar() == close || RightChar() == '-'))
                                        throw MakeException(RegexParseError.InvalidGroupName, "Invalid group name: Group names must begin with a word character.");
                                    if (capnum == 0)
                                        throw MakeException(RegexParseError.CapnumNotZero, "Capture number cannot be zero.");
                                }
                                else if (RegexCharClass.IsWordChar(ch))
                                {
                                    string capname = ScanCapname();

                                    if (IsCaptureName(capname))
                                        capnum = CaptureSlotFromName(capname);

                                    // check if we have bogus character after the name
                                    if (CharsRight > 0 && !(RightChar() == close || RightChar() == '-'))
                                        throw MakeException(RegexParseError.InvalidGroupName, "Invalid group name: Group names must begin with a word character.");
                                }
                                else if (ch == '-')
                                {
                                    proceed = true;
                                }
                                else
                                {
                                    // bad group name - starts with something other than a word character and isn't a number
                                    throw MakeException(RegexParseError.InvalidGroupName, "Invalid group name: Group names must begin with a word character.");
                                }

                                // grab part after - if any

                                if ((capnum != -1 || proceed == true) && CharsRight > 1 && RightChar() == '-')
                                {
                                    MoveRight();
                                    ch = RightChar();

                                    if (ch >= '0' && ch <= '9')
                                    {
                                        uncapnum = ScanDecimal();

                                        if (!IsCaptureSlot(uncapnum))
                                            throw MakeException(RegexParseError.UndefinedBackref, $"Reference to undefined group number {uncapnum}.");

                                        // check if we have bogus characters after the number
                                        if (CharsRight > 0 && RightChar() != close)
                                            throw MakeException(RegexParseError.InvalidGroupName, "Invalid group name: Group names must begin with a word character.");
                                    }
                                    else if (RegexCharClass.IsWordChar(ch))
                                    {
                                        string uncapname = ScanCapname();

                                        if (IsCaptureName(uncapname))
                                            uncapnum = CaptureSlotFromName(uncapname);
                                        else
                                            throw MakeException(RegexParseError.UndefinedNameRef, $"Reference to undefined group name '{uncapname}'.");

                                        // check if we have bogus character after the name
                                        if (CharsRight > 0 && RightChar() != close)
                                            throw MakeException(RegexParseError.InvalidGroupName, "Invalid group name: Group names must begin with a word character.");
                                    }
                                    else
                                    {
                                        // bad group name - starts with something other than a word character and isn't a number
                                        throw MakeException(RegexParseError.InvalidGroupName, "Invalid group name: Group names must begin with a word character.");
                                    }
                                }

                                // actually make the node

                                if ((capnum != -1 || uncapnum != -1) && CharsRight > 0 && RightCharMoveRight() == close)
                                {
                                    return new RegexNode(RegexNode.Capture, _options, capnum, uncapnum);
                                }
                                goto BreakRecognize;
                        }
                        break;

                    case '(':
                        // alternation construct (?(...) | )

                        int parenPos = Textpos;
                        if (CharsRight > 0)
                        {
                            ch = RightChar();

                            // check if the alternation condition is a backref
                            if (ch >= '0' && ch <= '9')
                            {
                                int capnum = ScanDecimal();
                                if (CharsRight > 0 && RightCharMoveRight() == ')')
                                {
                                    if (IsCaptureSlot(capnum))
                                        return new RegexNode(RegexNode.Testref, _options, capnum);
                                    else
                                        throw MakeException(RegexParseError.UndefinedReference, $"(?({capnum.ToString()}) ) reference to undefined group.");
                                }
                                else
                                    throw MakeException(RegexParseError.MalformedReference, $"(?({capnum.ToString()}) ) malformed.");
                            }
                            else if (RegexCharClass.IsWordChar(ch))
                            {
                                string capname = ScanCapname();

                                if (IsCaptureName(capname) && CharsRight > 0 && RightCharMoveRight() == ')')
                                    return new RegexNode(RegexNode.Testref, _options, CaptureSlotFromName(capname));
                            }
                        }
                        // not a backref
                        NodeType = RegexNode.Testgroup;
                        Textto(parenPos - 1);       // jump to the start of the parentheses
                        _ignoreNextParen = true;    // but make sure we don't try to capture the insides

                        int charsRight = CharsRight;
                        if (charsRight >= 3 && RightChar(1) == '?')
                        {
                            char rightchar2 = RightChar(2);
                            // disallow comments in the condition
                            if (rightchar2 == '#')
                                throw MakeException(RegexParseError.AlternationCantHaveComment, "Alternation conditions cannot be comments.");

                            // disallow named capture group (?<..>..) in the condition
                            if (rightchar2 == '\'')
                                throw MakeException(RegexParseError.AlternationCantCapture, "Alternation conditions do not capture and cannot be named.");
                            else
                            {
                                if (charsRight >= 4 && (rightchar2 == '<' && RightChar(3) != '!' && RightChar(3) != '='))
                                    throw MakeException(RegexParseError.AlternationCantCapture, "Alternation conditions do not capture and cannot be named.");
                            }
                        }

                        break;


                    default:
                        MoveLeft();

                        NodeType = RegexNode.Group;
                        // Disallow options in the children of a testgroup node
                        if (_group.NType != RegexNode.Testgroup)
                            ScanOptions();
                        if (CharsRight == 0)
                            goto BreakRecognize;

                        if ((ch = RightCharMoveRight()) == ')')
                            return null;

                        if (ch != ':')
                            goto BreakRecognize;
                        break;
                }

                return new RegexNode(NodeType, _options);
            }

        BreakRecognize:
            ;
            // break Recognize comes here

            throw MakeException(RegexParseError.UnrecognizedGrouping, "Unrecognized grouping construct.");
        }

        /// <summary>Scans whitespace or x-mode comments.</summary>
        private void ScanBlank()
        {
            if (IgnorePatternWhitespace)
            {
                for (; ;)
                {
                    while (CharsRight > 0 && IsWhitespace(RightChar()))
                        MoveRight();

                    if (CharsRight == 0)
                        break;

                    if (RightChar() == '#')
                    {
                        while (CharsRight > 0 && RightChar() != '\n')
                            MoveRight();
                    }
                    else if (CharsRight >= 3 && RightChar(2) == '#' &&
                             RightChar(1) == '?' && RightChar() == '(')
                    {
                        while (CharsRight > 0 && RightChar() != ')')
                            MoveRight();
                        if (CharsRight == 0)
                            throw MakeException(RegexParseError.UnterminatedComment, "Unterminated (?#...) comment.");
                        MoveRight();
                    }
                    else
                        break;
                }
            }
            else
            {
                for (; ;)
                {
                    if (CharsRight < 3 || RightChar(2) != '#' ||
                        RightChar(1) != '?' || RightChar() != '(')
                        return;

                    // skip comment (?# ...)
                    while (CharsRight > 0 && RightChar() != ')')
                        MoveRight();
                    if (CharsRight == 0)
                        throw MakeException(RegexParseError.UnterminatedComment, "Unterminated (?#...) comment.");
                    MoveRight();
                }
            }
        }

        /// <summary>Scans chars following a '\' (not counting the '\'), and returns a RegexNode for the type of atom scanned.</summary>
        private RegexNode ScanBackslash(bool scanOnly)
        {
            Debug.Assert(CharsRight > 0, "The current reading position must not be at the end of the pattern");

            char ch;
            switch (ch = RightChar())
            {
                case 'b':
                case 'B':
                case 'A':
                case 'G':
                case 'Z':
                case 'z':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    return new RegexNode(TypeFromCode(ch), _options);

                case 'w':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (IsEcmaScript)
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMAWordClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.WordClass);

                case 'W':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (IsEcmaScript)
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMAWordClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotWordClass);

                case 's':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (IsEcmaScript)
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMASpaceClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.SpaceClass);

                case 'S':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (IsEcmaScript)
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMASpaceClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotSpaceClass);

                case 'd':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (IsEcmaScript)
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.ECMADigitClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.DigitClass);

                case 'D':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    if (IsEcmaScript)
                        return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotECMADigitClass);
                    return new RegexNode(RegexNode.Set, _options, RegexCharClass.NotDigitClass);

                case 'p':
                case 'P':
                    MoveRight();
                    if (scanOnly)
                        return null;
                    var cc = new RegexCharClass();
                    cc.AddCategoryFromName(ParseProperty(), (ch != 'p'), IsCaseInsensitive, _pattern, _currentPos);
                    if (IsCaseInsensitive)
                        cc.AddLowercase(_culture);

                    return new RegexNode(RegexNode.Set, _options, cc.ToStringClass());

                default:
                    return ScanBasicBackslash(scanOnly);
            }
        }

        /// <summary>Scans \-style backreferences and character escapes</summary>
        private RegexNode ScanBasicBackslash(bool scanOnly)
        {
            if (CharsRight == 0)
                throw MakeException(RegexParseError.IllegalEndEscape, "Illegal \\ at end of pattern.");

            int backpos = Textpos;
            char close = '\0';
            bool angled = false;
            char ch = RightChar();

            // allow \k<foo> instead of \<foo>, which is now deprecated

            if (ch == 'k')
            {
                if (CharsRight >= 2)
                {
                    MoveRight();
                    ch = RightCharMoveRight();

                    if (ch == '<' || ch == '\'')
                    {
                        angled = true;
                        close = (ch == '\'') ? '\'' : '>';
                    }
                }

                if (!angled || CharsRight <= 0)
                    throw MakeException(RegexParseError.MalformedNameRef, "Malformed \\k<...> named back reference.");

                ch = RightChar();
            }

            // Note angle without \g

            else if ((ch == '<' || ch == '\'') && CharsRight > 1)
            {
                angled = true;
                close = (ch == '\'') ? '\'' : '>';

                MoveRight();
                ch = RightChar();
            }

            // Try to parse backreference: \<1>

            if (angled && ch >= '0' && ch <= '9')
            {
                int capnum = ScanDecimal();

                if (CharsRight > 0 && RightCharMoveRight() == close)
                {
                    if (scanOnly)
                        return null;
                    if (IsCaptureSlot(capnum))
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    else
                        throw MakeException(RegexParseError.UndefinedBackref, $"Reference to undefined group number {capnum.ToString()}.");
                }
            }

            // Try to parse backreference or octal: \1

            else if (!angled && ch >= '1' && ch <= '9')
            {
                if (IsEcmaScript)
                {
                    int capnum = -1;
                    int newcapnum = (int)(ch - '0');
                    int pos = Textpos - 1;
                    while (newcapnum <= _captop)
                    {
                        if (IsCaptureSlot(newcapnum) && (_caps == null || (int)_caps[newcapnum] < pos))
                            capnum = newcapnum;
                        MoveRight();
                        if (CharsRight == 0 || (ch = RightChar()) < '0' || ch > '9')
                            break;
                        newcapnum = newcapnum * 10 + (int)(ch - '0');
                    }
                    if (capnum >= 0)
                        return scanOnly ? null : new RegexNode(RegexNode.Ref, _options, capnum);
                }
                else
                {
                    int capnum = ScanDecimal();
                    if (scanOnly)
                        return null;
                    if (IsCaptureSlot(capnum))
                        return new RegexNode(RegexNode.Ref, _options, capnum);
                    else if (capnum <= 9)
                        throw MakeException(RegexParseError.UndefinedBackref, $"Reference to undefined group number {capnum.ToString()}.");
                }
            }


            // Try to parse backreference: \<foo>

            else if (angled && RegexCharClass.IsWordChar(ch))
            {
                string capname = ScanCapname();

                if (CharsRight > 0 && RightCharMoveRight() == close)
                {
                    if (scanOnly)
                        return null;
                    if (IsCaptureName(capname))
                        return new RegexNode(RegexNode.Ref, _options, CaptureSlotFromName(capname));
                    else
                        throw MakeException(RegexParseError.UndefinedNameRef, $"Reference to undefined group name '{capname}'.");
                }
            }

            // Not backreference: must be char code

            Textto(backpos);
            ch = ScanCharEscape();

            if (IsCaseInsensitive)
                ch = _culture.TextInfo.ToLower(ch);

            return scanOnly ? null : new RegexNode(RegexNode.One, _options, ch);
        }

        /// <summary>Scans a capture name: consumes word chars</summary>
        private string ScanCapname()
        {
            int startpos = Textpos;

            while (CharsRight > 0)
            {
                if (!RegexCharClass.IsWordChar(RightCharMoveRight()))
                {
                    MoveLeft();
                    break;
                }
            }

            return _pattern.Substring(startpos, Textpos - startpos);
        }


        /// <summary>Scans up to three octal digits (stops before exceeding 0377).</summary>
        private char ScanOctal()
        {
            // Consume octal chars only up to 3 digits and value 0377
            int c = 3;
            int d;
            int i;

            if (c > CharsRight)
                c = CharsRight;

            for (i = 0; c > 0 && unchecked((uint)(d = RightChar() - '0')) <= 7; c -= 1)
            {
                MoveRight();
                i *= 8;
                i += d;
                if (IsEcmaScript && i >= 0x20)
                    break;
            }

            // Octal codes only go up to 255.  Any larger and the behavior that Perl follows
            // is simply to truncate the high bits.
            i &= 0xFF;

            return (char)i;
        }

        /// <summary>Scans any number of decimal digits (pegs value at 2^31-1 if too large)</summary>
        private int ScanDecimal()
        {
            int i = 0;
            int d;

            while (CharsRight > 0 && unchecked((uint)(d = (char)(RightChar() - '0'))) <= 9)
            {
                MoveRight();

                if (i > (MaxValueDiv10) || (i == (MaxValueDiv10) && d > (MaxValueMod10)))
                    throw MakeException(RegexParseError.CaptureGroupOutOfRange, "Capture group numbers must be less than or equal to Int32.MaxValue.");

                i *= 10;
                i += d;
            }

            return i;
        }

        /// <summary>Scans exactly c hex digits (c=2 for \xFF, c=4 for \uFFFF)</summary>
        private char ScanHex(int c)
        {
            int i = 0;
            int d;

            if (CharsRight >= c)
            {
                for (; c > 0 && ((d = HexDigit(RightCharMoveRight())) >= 0); c -= 1)
                {
                    i *= 0x10;
                    i += d;
                }
            }

            if (c > 0)
                throw MakeException(RegexParseError.TooFewHex, "Insufficient hexadecimal digits.");

            return (char)i;
        }

        /// <summary>Returns n <= 0xF for a hex digit.</summary>
        private static int HexDigit(char ch)
        {
            int d;

            if ((uint)(d = ch - '0') <= 9)
                return d;

            if (unchecked((uint)(d = ch - 'a')) <= 5)
                return d + 0xa;

            if ((uint)(d = ch - 'A') <= 5)
                return d + 0xa;

            return -1;
        }

        /// <summary>Grabs and converts an ASCII control character</summary>
        private char ScanControl()
        {
            if (CharsRight == 0)
                throw MakeException(RegexParseError.MissingControl, "Missing control character.");

            char ch = RightCharMoveRight();

            // \ca interpreted as \cA

            if (ch >= 'a' && ch <= 'z')
                ch = (char)(ch - ('a' - 'A'));

            if (unchecked(ch = (char)(ch - '@')) < ' ')
                return ch;

            throw MakeException(RegexParseError.UnrecognizedControl, "Unrecognized control character.");
        }

        /// <summary>Returns true for options allowed only at the top level</summary>
        private bool IsOnlyTopOption(RegexOptions options)
        {
            return options == RegexOptions.RightToLeft ||
                options == RegexOptions.CultureInvariant ||
                options == RegexOptions.ECMAScript;
        }

        /// <summary>Scans cimsx-cimsx option string, stops at the first unrecognized char.</summary>
        private void ScanOptions()
        {
            for (bool off = false; CharsRight > 0; MoveRight())
            {
                char ch = RightChar();

                if (ch == '-')
                {
                    off = true;
                }
                else if (ch == '+')
                {
                    off = false;
                }
                else
                {
                    RegexOptions options = OptionFromCode(ch);
                    if (options == 0 || IsOnlyTopOption(options))
                        return;

                    if (off)
                        _options &= ~options;
                    else
                        _options |= options;
                }
            }
        }

        /// <summary>Scans \ code for escape codes that map to single Unicode chars.</summary>
        private char ScanCharEscape()
        {
            char ch = RightCharMoveRight();

            if (ch >= '0' && ch <= '7')
            {
                MoveLeft();
                return ScanOctal();
            }

            switch (ch)
            {
                case 'x':
                    return ScanHex(2);
                case 'u':
                    return ScanHex(4);
                case 'a':
                    return '\u0007';
                case 'b':
                    return '\b';
                case 'e':
                    return '\u001B';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'v':
                    return '\u000B';
                case 'c':
                    return ScanControl();
                default:
                    if (!IsEcmaScript && RegexCharClass.IsWordChar(ch))
                        throw MakeException(RegexParseError.UnrecognizedEscape, $"Unrecognized escape sequence \\{ch}.");
                    return ch;
            }
        }

        /// <summary>Scans X for \p{X} or \P{X}</summary>
        private string ParseProperty()
        {
            if (CharsRight < 3)
            {
                throw MakeException(RegexParseError.IncompleteSlashP, "Incomplete \\p{X} character escape.");
            }

            char ch = RightCharMoveRight();
            if (ch != '{')
            {
                throw MakeException(RegexParseError.MalformedSlashP, "Malformed \\p{X} character escape.");
            }

            int startpos = Textpos;
            while (CharsRight > 0)
            {
                ch = RightCharMoveRight();
                if (!(RegexCharClass.IsWordChar(ch) || ch == '-'))
                {
                    MoveLeft();
                    break;
                }
            }
            string capname = _pattern.Substring(startpos, Textpos - startpos);

            if (CharsRight == 0 || RightCharMoveRight() != '}')
                throw MakeException(RegexParseError.IncompleteSlashP, "Incomplete \\p{X} character escape.");

            return capname;
        }

        /// <summary>Returns ReNode type for zero-length assertions with a \ code.</summary>
        private int TypeFromCode(char ch)
        {
            switch (ch)
            {
                case 'b':
                    return IsEcmaScript ? RegexNode.ECMABoundary : RegexNode.Boundary;
                case 'B':
                    return IsEcmaScript ? RegexNode.NonECMABoundary : RegexNode.Nonboundary;
                case 'A':
                    return RegexNode.Beginning;
                case 'G':
                    return RegexNode.Start;
                case 'Z':
                    return RegexNode.EndZ;
                case 'z':
                    return RegexNode.End;
                default:
                    return RegexNode.Nothing;
            }
        }

        /// <summary>Returns option bit from single-char (?cimsx) code.</summary>
        private static RegexOptions OptionFromCode(char ch)
        {
            // case-insensitive
            if (ch >= 'A' && ch <= 'Z')
                ch += (char)('a' - 'A');

            switch (ch)
            {
                case 'i':
                    return RegexOptions.IgnoreCase;
                case 'r':
                    return RegexOptions.RightToLeft;
                case 'm':
                    return RegexOptions.Multiline;
                case 'n':
                    return RegexOptions.ExplicitCapture;
                case 's':
                    return RegexOptions.Singleline;
                case 'x':
                    return RegexOptions.IgnorePatternWhitespace;
#if DEBUG
                case 'd':
                    return RegexOptions.Debug;
#endif
                case 'e':
                    return RegexOptions.ECMAScript;
                default:
                    return 0;
            }
        }

        /// <summary>a prescanner for deducing the slots used for captures by doing a partial tokenization of the pattern.</summary>
        private void CountCaptures()
        {
            NoteCaptureSlot(0, 0);

            _autocap = 1;

            while (CharsRight > 0)
            {
                int pos = Textpos;
                char ch = RightCharMoveRight();
                switch (ch)
                {
                    case '\\':
                        if (CharsRight > 0)
                            ScanBackslash(scanOnly: true);
                        break;

                    case '#':
                        if (IgnorePatternWhitespace)
                        {
                            MoveLeft();
                            ScanBlank();
                        }
                        break;

                    case '[':
                        ScanCharClass(caseInsensitive: false, scanOnly: true);
                        break;

                    case ')':
                        if (!IsOptionsStackEmpty)
                            PopOptions();
                        break;

                    case '(':
                        if (CharsRight >= 2 && RightChar(1) == '#' && RightChar() == '?')
                        {
                            // we have a comment (?#
                            MoveLeft();
                            ScanBlank();
                        }
                        else
                        {
                            PushOptions();
                            if (CharsRight > 0 && RightChar() == '?')
                            {
                                // we have (?...
                                MoveRight();

                                if (CharsRight > 1 && (RightChar() == '<' || RightChar() == '\''))
                                {
                                    // named group: (?<... or (?'...

                                    MoveRight();
                                    ch = RightChar();

                                    if (ch != '0' && RegexCharClass.IsWordChar(ch))
                                    {
                                        if (ch >= '1' && ch <= '9')
                                            NoteCaptureSlot(ScanDecimal(), pos);
                                        else
                                            NoteCaptureName(ScanCapname(), pos);
                                    }
                                }
                                else
                                {
                                    // (?...

                                    // get the options if it's an option construct (?cimsx-cimsx...)
                                    ScanOptions();

                                    if (CharsRight > 0)
                                    {
                                        if (RightChar() == ')')
                                        {
                                            // (?cimsx-cimsx)
                                            MoveRight();
                                            PopKeepOptions();
                                        }
                                        else if (RightChar() == '(')
                                        {
                                            // alternation construct: (?(foo)yes|no)
                                            // ignore the next paren so we don't capture the condition
                                            _ignoreNextParen = true;

                                            // break from here so we don't reset _ignoreNextParen
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Simple (unnamed) capture group.
                                // Add unnamend parentheses if ExplicitCapture is not set
                                // and the next parentheses is not ignored.
                                if (!IsExplicitCapture && !_ignoreNextParen)
                                    NoteCaptureSlot(_autocap++, pos);
                            }
                        }

                        _ignoreNextParen = false;
                        break;
                }
            }

            AssignNameSlots();
        }

        /// <summary>Notes a used capture slot</summary>
        private void NoteCaptureSlot(int i, int pos)
        {
            if (!_caps.ContainsKey(i))
            {
                // the rhs of the hashtable isn't used in the parser

                _caps.Add(i, pos);
                _capcount++;

                if (_captop <= i)
                {
                    _captop = i == int.MaxValue ? i : i + 1;
                }
            }
        }

        /// <summary>Notes a used capture slot</summary>
        private void NoteCaptureName(string name, int pos)
        {
            if (_capnames == null)
            {
                _capnames = new Dictionary<string, int>();
                _capnamelist = new List<string>();
            }

            if (!_capnames.ContainsKey(name))
            {
                _capnames.Add(name, pos);
                _capnamelist.Add(name);
            }
        }

        /// <summary>Assigns unused slot numbers to the capture names</summary>
        private void AssignNameSlots()
        {
            if (_capnames != null)
            {
                for (int i = 0; i < _capnamelist.Count; i++)
                {
                    while (IsCaptureSlot(_autocap))
                        _autocap++;
                    string name = _capnamelist[i];
                    int pos = (int)_capnames[name];
                    _capnames[name] = _autocap;
                    NoteCaptureSlot(_autocap, pos);

                    _autocap++;
                }
            }

            // if the caps array has at least one gap, construct the list of used slots

            if (_capcount < _captop)
            {
                _capnumlist = new int[_capcount];
                int i = 0;

                // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                IDictionaryEnumerator de = _caps.GetEnumerator();
                while (de.MoveNext())
                {
                    _capnumlist[i++] = (int)de.Key;
                }

                Array.Sort(_capnumlist, Comparer<int>.Default);
            }

            // merge capsnumlist into capnamelist

            if (_capnames != null || _capnumlist != null)
            {
                List<string> oldcapnamelist;
                int next;
                int k = 0;

                if (_capnames == null)
                {
                    oldcapnamelist = null;
                    _capnames = new Dictionary<string, int>();
                    _capnamelist = new List<string>();
                    next = -1;
                }
                else
                {
                    oldcapnamelist = _capnamelist;
                    _capnamelist = new List<string>();
                    next = (int)_capnames[oldcapnamelist[0]];
                }

                for (int i = 0; i < _capcount; i++)
                {
                    int j = (_capnumlist == null) ? i : _capnumlist[i];

                    if (next == j)
                    {
                        _capnamelist.Add(oldcapnamelist[k++]);
                        next = (k == oldcapnamelist.Count) ? -1 : (int)_capnames[oldcapnamelist[k]];
                    }
                    else
                    {
                        string str = Convert.ToString(j, _culture);
                        _capnamelist.Add(str);
                        _capnames[str] = j;
                    }
                }
            }
        }

        private int CaptureSlotFromName(string capname) => _capnames[capname];

        /// <summary>True if the capture slot was noted</summary>
        private bool IsCaptureSlot(int i) => _caps?.ContainsKey(i) ?? i >= 0 && i < _capsize;

        private bool IsCaptureName(string capname) => _capnames != null && _capnames.ContainsKey(capname);

        private bool IsExplicitCapture => (_options & RegexOptions.ExplicitCapture) != 0;
        private bool IsCaseInsensitive => (_options & RegexOptions.IgnoreCase) != 0;
        private bool IsMultiLine => (_options & RegexOptions.Multiline) != 0;
        private bool IsSingleLine => (_options & RegexOptions.Singleline) != 0;
        private bool IgnorePatternWhitespace => (_options & RegexOptions.IgnorePatternWhitespace) != 0;
        private bool IsEcmaScript => (_options & RegexOptions.ECMAScript) != 0;

        private const byte Q = 5;    // quantifier
        private const byte S = 4;    // ordinary stopper
        private const byte Z = 3;    // ScanBlank stopper
        private const byte X = 2;    // whitespace
        private const byte E = 1;    // should be escaped

        /// <summary>For categorizing ASCII characters.</summary>
        private static readonly byte[] s_category = new byte[] {
            // 0 1 2 3 4 5 6 7 8 9 A B C D E F 0 1 2 3 4 5 6 7 8 9 A B C D E F
               0,0,0,0,0,0,0,0,0,X,X,0,X,X,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
            //   ! " # $ % & ' ( ) * + , - . / 0 1 2 3 4 5 6 7 8 9 : ; < = > ?
               X,0,0,Z,S,0,0,0,S,S,Q,Q,0,0,S,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,
            // @ A B C D E F G H I J K L M N O P Q R S T U V W X Y Z [ \ ] ^ _
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,S,S,0,S,0,
            // ' a b c d e f g h i j k l m n o p q r s t u v w x y z { | } ~
               0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,Q,S,0,0,0};

        /// <summary>Returns true for those characters that terminate a string of ordinary chars.</summary>
        private static bool IsSpecial(char ch) => (ch <= '|' && s_category[ch] >= S);

        /// <summary>Returns true for those characters that terminate a string of ordinary chars.</summary>
        private static bool IsStopperX(char ch) => (ch <= '|' && s_category[ch] >= X);

        /// <summary>Returns true for those characters that begin a quantifier.</summary>
        private static bool IsQuantifier(char ch) => (ch <= '{' && s_category[ch] >= Q);

        private bool IsTrueQuantifier()
        {
            Debug.Assert(CharsRight > 0, "The current reading position must not be at the end of the pattern");

            int startpos = Textpos;
            char ch = CharAt(startpos);
            if (ch != '{')
                return ch <= '{' && s_category[ch] >= Q;

            int pos = startpos;
            int nChars = CharsRight;
            while (--nChars > 0 && (ch = CharAt(++pos)) >= '0' && ch <= '9') ;

            if (nChars == 0 || pos - startpos == 1)
                return false;

            if (ch == '}')
                return true;

            if (ch != ',')
                return false;

            while (--nChars > 0 && (ch = CharAt(++pos)) >= '0' && ch <= '9') ;

            return nChars > 0 && ch == '}';
        }

        private static bool IsWhitespace(char ch) => (ch <= ' ' && s_category[ch] == X);

        /// <summary>Returns true for chars that should be escaped.</summary>
        private static bool IsMetachar(char ch) => (ch <= '|' && s_category[ch] >= E);


        /// <summary>Add a string to the last concatenate.</summary>
        private void AddConcatenate(int pos, int cch, bool isReplacement)
        {
            if (cch == 0)
                return;

            RegexNode node;
            if (cch > 1)
            {
                string str;
                if (IsCaseInsensitive && !isReplacement)
                {
                    str = _pattern.Substring(pos, cch).ToLower(_culture);
                }
                else
                {
                    str = _pattern.Substring(pos, cch);
                }

                node = new RegexNode(RegexNode.Multi, _options, str);
            }
            else
            {
                char ch = _pattern[pos];

                if (IsCaseInsensitive && !isReplacement)
                    ch = _culture.TextInfo.ToLower(ch);

                node = new RegexNode(RegexNode.One, _options, ch);
            }

            _concatenation.AddChild(node);
        }

        /// <summary>Push the parser state (in response to an open paren)</summary>
        private void PushGroup()
        {
            _group.Next = _stack;
            _alternation.Next = _group;
            _concatenation.Next = _alternation;
            _stack = _concatenation;
        }

        /// <summary>Remember the pushed state (in response to a ')')</summary>
        private void PopGroup()
        {
            _concatenation = _stack;
            _alternation = _concatenation.Next;
            _group = _alternation.Next;
            _stack = _group.Next;

            // The first () inside a Testgroup group goes directly to the group
            if (_group.Type() == RegexNode.Testgroup && _group.ChildCount() == 0)
            {
                if (_unit == null)
                    throw MakeException(RegexParseError.IllegalCondition, "Illegal conditional (?(...)) expression.");

                _group.AddChild(_unit);
                _unit = null;
            }
        }

        /// <summary>True if the group stack is empty.</summary>
        private bool EmptyStack() => _stack == null;

        /// <summary>
        ///             Start a new round for the parser state (in response to an open paren or string start)
        /// </summary>
        private void StartGroup(RegexNode openGroup)
        {
            _group = openGroup;
            _alternation = new RegexNode(RegexNode.Alternate, _options);
            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /// <summary>Finish the current concatenation (in response to a |)</summary>
        private void AddAlternate()
        {
            // The | parts inside a Testgroup group go directly to the group

            if (_group.Type() == RegexNode.Testgroup || _group.Type() == RegexNode.Testref)
            {
                _group.AddChild(_concatenation.ReverseLeft());
            }
            else
            {
                _alternation.AddChild(_concatenation.ReverseLeft());
            }

            _concatenation = new RegexNode(RegexNode.Concatenate, _options);
        }

        /// <summary>Finish the current quantifiable (when a quantifier is not found or is not possible)</summary>
        private void AddConcatenate()
        {
            // The first (| inside a Testgroup group goes directly to the group

            _concatenation.AddChild(_unit);
            _unit = null;
        }

        /// <summary>Finish the current quantifiable (when a quantifier is found)</summary>
        private void AddConcatenate(bool lazy, int min, int max)
        {
            _concatenation.AddChild(_unit.MakeQuantifier(lazy, min, max));
            _unit = null;
        }

        /// <summary>Returns the current unit</summary>
        private RegexNode Unit()
        {
            return _unit;
        }

        /// <summary>Sets the current unit to a single char node</summary>
        private void AddUnitOne(char ch)
        {
            if (IsCaseInsensitive)
                ch = _culture.TextInfo.ToLower(ch);

            _unit = new RegexNode(RegexNode.One, _options, ch);
        }

        /// <summary>Sets the current unit to a single inverse-char node</summary>
        private void AddUnitNotone(char ch)
        {
            if (IsCaseInsensitive)
                ch = _culture.TextInfo.ToLower(ch);

            _unit = new RegexNode(RegexNode.Notone, _options, ch);
        }

        /// <summary>Sets the current unit to a single set node</summary>
        private void AddUnitSet(string cc)
        {
            _unit = new RegexNode(RegexNode.Set, _options, cc);
        }

        /// <summary>Sets the current unit to a subtree</summary>
        private void AddUnitNode(RegexNode node)
        {
            _unit = node;
        }

        /// <summary>Sets the current unit to an assertion of the specified type</summary>
        private void AddUnitType(int type)
        {
            _unit = new RegexNode(type, _options);
        }

        /// <summary>Finish the current group (in response to a ')' or end)</summary>
        private void AddGroup()
        {
            if (_group.Type() == RegexNode.Testgroup || _group.Type() == RegexNode.Testref)
            {
                _group.AddChild(_concatenation.ReverseLeft());

                if (_group.Type() == RegexNode.Testref && _group.ChildCount() > 2 || _group.ChildCount() > 3)
                    throw MakeException(RegexParseError.TooManyAlternates, "Too many | in (?()|).");
            }
            else
            {
                _alternation.AddChild(_concatenation.ReverseLeft());
                _group.AddChild(_alternation);
            }

            _unit = _group;
        }

        /// <summary>Saves options on a stack.</summary>
        private void PushOptions()
        {
            _optionsStack.Push(_options);
        }

        /// <summary>Recalls options from the stack.</summary>
        private void PopOptions()
        {
            _options = _optionsStack.Pop();
        }

        /// <summary>True if options stack is empty.</summary>
        private bool IsOptionsStackEmpty => _optionsStack.Count == 0;

        /// <summary>Pops the options stack, but keeps the current options unchanged.</summary>
        private void PopKeepOptions()
        {
            _ = _optionsStack.Pop();
        }

        /// <summary>Fills in a RegexParseException</summary>
        private RegexParseException MakeException(RegexParseError error, string message) => new RegexParseException(error, _currentPos, $"Invalid pattern '{_pattern}' at offset {_currentPos}. {message}");

        /// <summary>Returns the current parsing position.</summary>
        private int Textpos => _currentPos;

        /// <summary>Zaps to a specific parsing position.</summary>
        private void Textto(int pos)
        {
            _currentPos = pos;
        }

        /// <summary>Returns the char at the right of the current parsing position and advances to the right.</summary>
        private char RightCharMoveRight() => _pattern[_currentPos++];

        /// <summary>Moves the current position to the right.</summary>
        private void MoveRight()
        {
            MoveRight(1);
        }

        private void MoveRight(int i)
        {
            _currentPos += i;
        }

        /// <summary>Moves the current parsing position one to the left.</summary>
        private void MoveLeft()
        {
            --_currentPos;
        }

        /// <summary>Returns the char left of the current parsing position.</summary>

        private char CharAt(int i) => _pattern[i];

        /// <summary>Returns the char right of the current parsing position.</summary>
        internal char RightChar() => _pattern[_currentPos];

        /// <summary>Returns the char i chars right of the current parsing position.</summary>
        private char RightChar(int i) => _pattern[_currentPos + i];

        /// <summary>Number of characters to the right of the current parsing position.</summary>
        private int CharsRight => _pattern.Length - _currentPos;
    }
}