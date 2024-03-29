﻿using System;
using System.Linq;

namespace NetRegexCompiler.Compiler.Text.RegularExpressions
{
    partial class RegexCSharpCompiler
    {
        private int Operand(int i) => CurrentOperation.Operands[i];

        private void Backtrack() => Writer.Write($"goto backtrack;");

        private void Goto(int operationPos)
        {
            var operation = Operations.Single(op => op.Index == operationPos);
            // when branching backward, ensure storage
            if (operation.Index < CurrentOperation.Index)
                Writer.Write($"{EnsureStorage}()");
            Writer.Write($"goto {operation.Label};");
        }

        private void GotoNextOperation()
        {
            Writer.Write($"goto {Operations[CurrentOperation.Id + 1].Label}");
        }
        
        private FormattableString IsMatched(int cap) => $"IsMatched({cap})";
        private FormattableString MatchIndex(int cap) => $"MatchIndex({cap})";
        private FormattableString MatchLength(int cap) => $"MatchLength({cap})";

        private void StackPop(int framesize = 1)
        {
            if (framesize == 1)
                Writer.Write($"{runstackpos}++");
            else
                Writer.Write($"{runstackpos} += {framesize}");
        }

        private FormattableString StackPeek(int i = 0) => $"{runstack}[{runstackpos} - {i + 1}]";

        private void StackPush(FormattableString I1) => StackPush((object)I1);
        private void StackPush(FormattableString I1, FormattableString I2) => StackPush((object)I1, (object)I2);
        private void StackPush(FormattableString I1, object I2) => StackPush((object)I1, (object)I2);
        private void StackPush(object I1, FormattableString I2) => StackPush((object)I1, (object)I2);
        private void StackPush(params object[] IX)
        {
            foreach (var I in IX)
                Writer.Write($"{runstack}[--{runstackpos}] = {I}");
        }
        
        private void TrackPop(int framesize = 1)
        {
            if (framesize == 1)
                Writer.Write($"{runtrackpos}++");
            else
                Writer.Write($"{runtrackpos} += {framesize}");
        }

        private FormattableString TrackPeek(int i = 0) => $"{runtrack}[{runtrackpos} - {i + 1}]";

        private void TrackPush(FormattableString I1) => TrackPush((object)I1);
        private void TrackPush(FormattableString I1, FormattableString I2) => TrackPush((object)I1, (object)I2);
        private void TrackPush(FormattableString I1, object I2) => TrackPush((object)I1, (object)I2);
        private void TrackPush(object I1, FormattableString I2) => TrackPush((object)I1, (object)I2);
        private void TrackPush(FormattableString I1, FormattableString I2, FormattableString I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(FormattableString I1, FormattableString I2, object I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(FormattableString I1, object I2, FormattableString I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(FormattableString I1, object I2, object I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(object I1, FormattableString I2, FormattableString I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(object I1, FormattableString I2, object I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(object I1, object I2, FormattableString I3) => TrackPush((object)I1, (object)I2, (object)I3);
        private void TrackPush(params object[] IX)
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: false);
            foreach (var I in IX)
                Writer.Write($"{runtrack}[--{runtrackpos}] = {I}");
            Writer.Write($"{runtrack}[--{runtrackpos}] = {backtrackOp.Id}");
        }

        private void TrackPush2(FormattableString I1) => TrackPush2((object)I1);
        private void TrackPush2(FormattableString I1, FormattableString I2) => TrackPush2((object)I1, (object)I2);
        private void TrackPush2(FormattableString I1, object I2) => TrackPush2((object)I1, (object)I2);
        private void TrackPush2(object I1, FormattableString I2) => TrackPush2((object)I1, (object)I2);
        private void TrackPush2(params object[] IX)
        {
            var backtrackOp = BacktrackOperations.Add(CurrentOperation, isBack2: true);
            foreach (var I in IX)
                Writer.Write($"{runtrack}[--{runtrackpos}] = {I}");
            Writer.Write($"{runtrack}[--{runtrackpos}] = {backtrackOp.Id}");
        }

        private FormattableString Trackpos() => $"{runtrack}.Length - {runtrackpos}";

        private FormattableString Popcrawl() => $"{runcrawl}[{runcrawlpos}++]";

        private FormattableString Crawlpos() => $"{runcrawl}.Length - {runcrawlpos}";

        private FormattableString Textpos() => $"{runtextpos}";
        private FormattableString Textstart() => $"{runtextstart}";

        private void Trackto(FormattableString pos) => Trackto((object) pos);
        private void Trackto(object pos)
        {
            Writer.Write($"{runtrackpos} = {runtrack}.Length - {pos}");
        }

        private void Textto(FormattableString pos) => Textto((object) pos);
        private void Textto(object pos)
        {
            Writer.Write($"{runtextpos} = {pos}");
        }

        private void TransferCapture(int capnum, int uncapnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"TransferCapture({capnum}, {uncapnum}, {start}, {end})");
        }

        private void Capture(int capnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"Capture({capnum}, {start}, {end})");
        }

        private void TransferCapture(FormattableString capnum, FormattableString uncapnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"TransferCapture({capnum}, {uncapnum}, {start}, {end})");
        }

        private void Capture(FormattableString capnum, FormattableString start, FormattableString end)
        {
            Writer.Write($"Capture({capnum}, {start}, {end})");
        }

        private void Uncapture()
        {
            Writer.Write($"Uncapture();");
        }

        private FormattableString Leftchars() => $"{runtextpos} - {runtextbeg}";
        private FormattableString Rightchars() => $"{runtextend} - {runtextpos}";

        private FormattableString IsBoundary(FormattableString index, object startpos, object endpos)
        {
            return $"IsBoundary({index}, {startpos}, {endpos})";
            //return $"({index} > {startpos} && {IsWordChar($"{runtext}[{index} - 1]")}) != ({index} < {endpos} && {IsWordChar($"{runtext}[{index}]")})";
        }

        private FormattableString IsECMABoundary(FormattableString index, object startpos, object endpos)
        {
            return $"IsECMABoundary({index}, {startpos}, {endpos})";
            //return $"({index} > {startpos} && {IsECMAWordChar($"{runtext}[{index} - 1]")}) != ({index} < {endpos} && {IsECMAWordChar($"{runtext}[{index}]")})";
        }

        // TODO: We can almost certainly do some work to improve IsWordChar
        private FormattableString IsWordChar(FormattableString expr) => $"RegexCharClass.IsWordChar({expr})";
        private FormattableString IsECMAWordChar(FormattableString expr) => $"RegexCharClass.IsECMAWordChar({expr})";

        // TODO: Definitely stuff we can do to pre-preprocess the class here to eliminate work
        private FormattableString CharInClass(FormattableString expr, string str) => $@"CharInClass({expr}, ""{str}"")";

        private int Bump() => IsRightToLeft ? -1 : 1;
    }
}
