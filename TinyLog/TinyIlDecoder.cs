using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace TinyIlDecoder
{
    public static class Extensions
    {
        public static void TrimEnd(this StringBuilder builder, string end)
        {
            while (true)
            {
                if (builder.Length < end.Length)
                {
                    return;
                }
                var n = builder.Length - end.Length;
                for (int i = 0; i < end.Length; i++)
                {
                    if (builder[n + i] != end[i])
                    {
                        return;
                    }
                }
                builder.Length -= end.Length;
            }
        }

        public static byte[] Slice(this byte[] bytes, int startIndex, int length)
        {
            var ret = new byte[length];
            for (int i = 0; i < length; i++)
            {
                ret[i] = bytes[startIndex + i];
            }
            return ret;
        }

        public static unsafe int ReadInt(this byte[] bytes, int position = 0)
        {
            fixed (byte* p = bytes)
            {
                var a = *((int*)(p + position));
                return a;
            }
        }

        public static unsafe long ReadLong(this byte[] bytes, int position = 0)
        {
            fixed (byte* p = bytes)
            {
                var a = *((long*)(p + position));
                return a;
            }
        }

        public static unsafe float ReadFloat(this byte[] bytes, int position = 0)
        {
            fixed (byte* p = bytes)
            {
                var a = *((float*)(p + position));
                return a;
            }
        }

        public static unsafe double ReadDouble(this byte[] bytes, int position = 0)
        {
            fixed (byte* p = bytes)
            {
                var a = *((double*)(p + position));
                return a;
            }
        }

        public static unsafe short ReadShort(this byte[] bytes, int position = 0)
        {
            fixed (byte* p = bytes)
            {
                var a = *((short*)(p + position));
                return a;
            }
        }

        public static byte ReadByte(this byte[] bytes, int position = 0)
        {
            return bytes[position];
        }
    }

    public class OpInfo
    {
        public IlReader Reader { get; }
        public bool Eof => Reader.Position >= Reader.Length;

        public OpInfo(byte[] body, int position = 0)
        {
            Reader = new IlReader(body, position);
        }

        public virtual int MaxStackSize => 0;
        public virtual IList<LocalVariableInfo> LocalVariables => new List<LocalVariableInfo>();
        public virtual bool InitLocals => false;

        public virtual void AppendMethod(StringBuilder builder)
        {
            Reader.ReadInt();
            builder.Append("[method]");
        }

        public virtual void AppendField(StringBuilder builder)
        {
            Reader.ReadInt();
            builder.Append("[field]");
        }

        public virtual void AppendType(StringBuilder builder)
        {
            Reader.ReadInt();
            builder.Append("[type]");
        }

        public virtual void AppendMember(StringBuilder builder)
        {
            Reader.ReadInt();
            builder.Append("[member]");
        }

        public virtual void AppendString(StringBuilder builder)
        {
            Reader.ReadInt();
            builder.Append("[string]");
        }

        public virtual void AppendSignature(StringBuilder builder)
        {
            Reader.ReadInt();
            builder.Append("[signature]");
        }
    }

    public class RuntimeOpInfo : OpInfo
    {
        private static readonly Dictionary<Type, string> CommonTypes;

        static RuntimeOpInfo()
        {
            CommonTypes = new Dictionary<Type, string>
            {
                {typeof (int), "int"},
                {typeof (uint), "uint"},
                {typeof (long), "long"},
                {typeof (ulong), "ulong"},
                {typeof (short), "short"},
                {typeof (ushort), "ushort"},
                {typeof (sbyte), "sbyte"},
                {typeof (byte), "byte"},
                {typeof (float), "float"},
                {typeof (double), "double"},
                {typeof (bool), "bool"},
                {typeof (string), "string"},
                {typeof (char), "char"},
            };
        }

        public readonly MethodBase Method;
        private readonly MethodBody _body;
        private readonly Module _module;
        private readonly Type[] _typeArgs;
        private readonly Type[] _methodArgs;
        public override int MaxStackSize => _body.MaxStackSize;
        public override IList<LocalVariableInfo> LocalVariables => _body.LocalVariables;
        public override bool InitLocals => _body.InitLocals;

        public RuntimeOpInfo(MethodBase method, int position = 0) : this(method, method.GetMethodBody(), position)
        {
        }

        private RuntimeOpInfo(MethodBase method, MethodBody body, int position) : base(body?.GetILAsByteArray(), position)
        {
            Method = method;
            _body = body;
            _module = method.Module;
            try
            {
                _typeArgs = method.DeclaringType?.GetGenericArguments();
            }
            catch (Exception)
            {
                // leave it be null
            }
            try
            {
                _methodArgs = method.GetGenericArguments();
            }
            catch (Exception)
            {
                // leave it be null
            }
        }

        protected void AppendFullType(StringBuilder builder, Type type)
        {
            string name;
            if (CommonTypes.TryGetValue(type, out name))
            {
                builder.Append(name);
            }
            else
            {
                builder.Append("[").Append(type.Assembly.GetName().Name).Append("]").Append(type.FullName ?? type.Name);
            }
        }

        private void AppendMethod(StringBuilder builder, MethodBase method)
        {
            var type = method.DeclaringType;
            if (type != null)
            {
                AppendFullType(builder, type);
                builder.Append(".").Append(method.Name).Append("(");
                foreach (var p in method.GetParameters())
                {
                    if (p.IsIn)
                    {
                        builder.Append("ref ");
                    }
                    if (p.IsOut)
                    {
                        builder.Append("out ");
                    }
                    AppendFullType(builder, p.ParameterType);
                    builder.Append(", ");
                }
                builder.TrimEnd(", ");
                builder.Append(")");
            }
        }

        public override void AppendMethod(StringBuilder builder)
        {
            var method = ResolveMethod();
            AppendMethod(builder, method);
        }

        private MethodBase ResolveMethod()
        {
            var token = Reader.ReadInt();
            return _module.ResolveMethod(token, _typeArgs, _methodArgs);
        }

        private void AppendField(StringBuilder builder, FieldInfo field)
        {
            var type = field?.DeclaringType;
            if (type != null)
            {
                AppendFullType(builder, field.FieldType);
                builder.Append(" ");
                AppendFullType(builder, type);
                builder.Append("::");
                builder.Append(field.Name);
            }
        }

        public override void AppendField(StringBuilder builder)
        {
            var field = ResolveField();
            AppendField(builder, field);
        }

        private FieldInfo ResolveField()
        {
            var token = Reader.ReadInt();
            return _module.ResolveField(token, _typeArgs, _methodArgs);
        }

        public override void AppendString(StringBuilder builder)
        {
            var s = ResolveString();
            builder.Append("\"").Append(s).Append("\"");
        }

        private string ResolveString()
        {
            var token = Reader.ReadInt();
            return _module.ResolveString(token);
        }

        public override void AppendType(StringBuilder builder)
        {
            var t = ResolveType();
            AppendFullType(builder, t);
        }

        private Type ResolveType()
        {
            var token = Reader.ReadInt();
            return _module.ResolveType(token, _typeArgs, _methodArgs);
        }

        public override void AppendMember(StringBuilder builder)
        {
            var member = ResolveMember();
            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    AppendMethod(builder, (MethodBase)member);
                    return;
                case MemberTypes.Field:
                    AppendField(builder, (FieldInfo)member);
                    return;
                case MemberTypes.Custom:
                case MemberTypes.Event:
                case MemberTypes.Property:
                    builder.Append(member.Name);
                    return;
                case MemberTypes.Method:
                    AppendMethod(builder, (MethodBase)member);
                    return;
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    builder.Append(((Type)member).FullName ?? ((Type)member).Name);
                    return;
            }
        }

        private MemberInfo ResolveMember()
        {
            var token = Reader.ReadInt();
            return _module.ResolveMember(token, _typeArgs, _methodArgs);
        }

        public override void AppendSignature(StringBuilder builder)
        {
            var sig = ResolveSignature();
            builder.Append("[");
            foreach (var t in sig)
            {
                builder.Append($"{t:X2}");
            }
            builder.Append("]");
        }

        private byte[] ResolveSignature()
        {
            var token = Reader.ReadInt();
            return _module.ResolveSignature(token);
        }
    }

    public class IlReader
    {
        private static readonly Dictionary<short, OpCode> OpCodeDic;

        static IlReader()
        {
            OpCodeDic = new Dictionary<short, OpCode>();
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var oc = field.GetValue(null);
                if (oc is OpCode)
                {
                    var c = (OpCode)oc;
                    OpCodeDic.Add(c.Value, c);
                }
            }
        }

        private readonly byte[] _il;
        public int Position { get; private set; }
        public int LastOpCodeStartIndex { get; private set; }
        public int LastOpCodeSize { get; private set; }
        public readonly int Length;

        public IlReader(byte[] il, int position = 0)
        {
            _il = il;
            if (_il == null)
            {
                throw new NullReferenceException("Can not get IL bytes.");
            }
            Position = position;
            LastOpCodeStartIndex = 0;
            LastOpCodeSize = 0;
            Length = _il.Length;
        }

        public IEnumerable<byte> GetFullOpCodeBytes()
        {
            for (int i = LastOpCodeStartIndex; i < Position; i++)
            {
                yield return _il[i];
            }
        }

        public OpCode ReadOpCode()
        {
            LastOpCodeStartIndex = Position;
            short highByte = _il[Position++];
            short lowByte = 0;
            if (highByte == 0xFE)
            {
                lowByte = _il[Position++];
                highByte <<= 8;
            }
            var ocValue = (short)(highByte | lowByte);
            return OpCodeDic[ocValue];
        }

        public int ReadInt()
        {
            var n = _il.ReadInt(Position);
            Position += 4;
            return n;
        }

        public long ReadLong()
        {
            var n = _il.ReadLong(Position);
            Position += 8;
            return n;
        }

        public float ReadFloat()
        {
            var n = _il.ReadFloat(Position);
            Position += 4;
            return n;
        }

        public double ReadDouble()
        {
            var n = _il.ReadDouble(Position);
            Position += 8;
            return n;
        }

        public byte ReadByte()
        {
            var n = _il.ReadByte(Position);
            Position++;
            return n;
        }

        public short ReadShort()
        {
            var n = _il.ReadShort(Position);
            Position += 2;
            return n;
        }
    }

    public class SimpleMethodProcessor
    {
        protected readonly OpInfo Info;
        protected readonly StringBuilder Builder;
        protected readonly bool AddIlBytes;

        public SimpleMethodProcessor(byte[] body, bool addIlBytes) : this(new OpInfo(body), addIlBytes)
        {
        }

        public SimpleMethodProcessor(OpInfo info, bool addIlBytes)
        {
            Builder = new StringBuilder();
            Info = info;
            AddIlBytes = addIlBytes;
        }

        public string Decode()
        {
            try
            {
                Builder.Length = 0;
                AppendMethodSignature();
                ProcessByteCodes();
                return Builder.ToString();
            }
            catch (NullReferenceException ex)
            {
                return ex.Message;
            }
            catch (Exception e)
            {
                return e.Message + "\n" + Builder;
            }
        }

        private void ProcessByteCodes()
        {
            Builder.AppendLine("{");
            ProcessVariables();
            ProcessMaxStack();
            while (!Info.Eof)
            {
                Builder.Append($"  {Info.Reader.Position:X4}: ");
                var p = new OpCodeProcessor(Builder, Info, AddIlBytes);
                p.Append();
                Builder.AppendLine();
            }
            Builder.AppendLine("}");
        }

        private void ProcessMaxStack()
        {
            Builder.Append("  .maxstack ")
                .AppendLine(Info.MaxStackSize.ToString())
                .AppendLine();
        }

        private void ProcessVariables()
        {
            if (Info.LocalVariables.Count > 0)
            {
                Builder.Append("  .locals ")
                    .Append(Info.InitLocals ? "init " : "")
                    .AppendLine("(");
                foreach (var v in Info.LocalVariables)
                {
                    Builder.Append("    [").Append(v.LocalIndex).Append("] ").AppendLine(v.LocalType?.ToString());
                }
                Builder.AppendLine("  )");
            }
        }

        protected virtual void AppendMethodSignature()
        {
        }
    }

    public class MethodProcessor : SimpleMethodProcessor
    {
        private readonly MethodInfo _method;

        public MethodProcessor(MethodInfo method) : base(new RuntimeOpInfo(method), false)
        {
            _method = method;
        }

        protected override void AppendMethodSignature()
        {
            Builder.Append(".method ").Append(_method.Name);
            Builder.Append("(");
            foreach (var p in _method.GetParameters())
            {
                Builder.Append(p.ParameterType).Append(",");
            }
            Builder.TrimEnd(",");
            Builder.AppendLine(")");
        }
    }

    public class OpCodeProcessor
    {
        private readonly StringBuilder _builder;
        private readonly OpInfo _info;
        private readonly bool _addIlBytes;

        public OpCodeProcessor(StringBuilder builder, OpInfo info, bool addIlBytes)
        {
            _builder = builder;
            _info = info;
            _addIlBytes = addIlBytes;
        }

        public void Append(string endOfLine = null)
        {
            if (!_info.Eof)
            {
                var opCode = _info.Reader.ReadOpCode();
                var opProcessor = GetOpProcessor(opCode.OperandType, _builder, _info);
                _builder.Append(opCode.Name).Append(" ");
                opProcessor.ReadOperand();
                AppendIlBytes();
                _builder.TrimEnd(" ");
                if (endOfLine != null)
                {
                    _builder.Append(endOfLine);
                }
            }
        }

        private void AppendIlBytes()
        {
            if (_addIlBytes)
            {
                _builder.TrimEnd(" ");
                _builder.Append(" $");
                foreach (byte t in _info.Reader.GetFullOpCodeBytes())
                {
                    _builder.Append($"{t:X2}");
                }
            }
        }

        private static OperandProcessor GetOpProcessor(OperandType opType, StringBuilder builder, OpInfo info)
        {
            switch (opType)
            {
                case OperandType.InlineMethod:
                    return new OperandInlineMethodProcessor(builder, info);
                case OperandType.InlineString:
                    return new OperandInlineStringProcessor(builder, info);
                case OperandType.InlineNone:
                    return new OperandInlineNoneProcessor(builder, info);
                case OperandType.ShortInlineBrTarget:
                    return new OperandShortInlineBrTargetProcessor(builder, info);
                case OperandType.InlineI:
                    return new OperandInlineIProcessor(builder, info);
                case OperandType.InlineI8:
                    return new OperandInlineI8Processor(builder, info);
                case OperandType.ShortInlineR:
                    return new OperandShortInlineRProcessor(builder, info);
                case OperandType.InlineR:
                    return new OperandInlineRProcessor(builder, info);
                case OperandType.InlineType:
                    return new OperandInlineTypeProcessor(builder, info);
                case OperandType.ShortInlineVar:
                    return new OperandShortInlineVarProcessor(builder, info);
                case OperandType.InlineField:
                    return new OperandInlineFieldProcessor(builder, info);
                case OperandType.InlineBrTarget:
                    return new OperandInlineBrTargetProcessor(builder, info);
                case OperandType.InlineSig:
                    return new OperandInlineSigProcessor(builder, info);
                case OperandType.InlineSwitch:
                    return new OperandInlineSwitchProcessor(builder, info);
                case OperandType.InlineTok:
                    return new OperandInlineTokProcessor(builder, info);
                case OperandType.InlineVar:
                    return new OperandInlineVarProcessor(builder, info);
                case OperandType.ShortInlineI:
                    return new OperandShortInlineIProcessor(builder, info);
            }
            throw new SystemException("Unknown Operand type.");
        }
    }

    public abstract class OperandProcessor
    {
        private static readonly Dictionary<Type, string> CommonTypes;

        static OperandProcessor()
        {
            CommonTypes = new Dictionary<Type, string>
            {
                {typeof (int), "int"},
                {typeof (uint), "uint"},
                {typeof (long), "long"},
                {typeof (ulong), "ulong"},
                {typeof (short), "short"},
                {typeof (ushort), "ushort"},
                {typeof (sbyte), "sbyte"},
                {typeof (byte), "byte"},
                {typeof (float), "float"},
                {typeof (double), "double"},
                {typeof (bool), "bool"},
                {typeof (string), "string"},
                {typeof (char), "char"},
            };
        }

        protected StringBuilder Builder;
        protected OpInfo Info;

        protected OperandProcessor(StringBuilder builder, OpInfo info)
        {
            Builder = builder;
            Info = info;
        }

        public abstract void ReadOperand();

        protected void AppendFullType(Type type)
        {
            string name;
            if (CommonTypes.TryGetValue(type, out name))
            {
                Builder.Append(name);
            }
            else
            {
                Builder.Append("[").Append(type.Assembly.GetName().Name).Append("]").Append(type.FullName ?? type.Name);
            }
        }

        protected void AppendMethod(MethodBase method)
        {
            var type = method.DeclaringType;
            if (type != null)
            {
                AppendFullType(type);
                Builder.Append(".").Append(method.Name).Append("(");
                foreach (var p in method.GetParameters())
                {
                    if (p.IsIn)
                    {
                        Builder.Append("ref ");
                    }
                    if (p.IsOut)
                    {
                        Builder.Append("out ");
                    }
                    AppendFullType(p.ParameterType);
                    Builder.Append(", ");
                }
                Builder.TrimEnd(", ");
                Builder.Append(")");
            }
        }

        protected void AppendField(FieldInfo field)
        {
            var type = field?.DeclaringType;
            if (type != null)
            {
                AppendFullType(field.FieldType);
                Builder.Append(" ");
                AppendFullType(type);
                Builder.Append("::");
                Builder.Append(field.Name);
            }
        }
    }

    public class OperandInlineMethodProcessor : OperandProcessor
    {
        public OperandInlineMethodProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            Info.AppendMethod(Builder);
        }
    }

    public class OperandInlineStringProcessor : OperandProcessor
    {
        public OperandInlineStringProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            Info.AppendString(Builder);
        }
    }

    public class OperandInlineNoneProcessor : OperandProcessor
    {
        public OperandInlineNoneProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
        }
    }

    public class OperandShortInlineBrTargetProcessor : OperandProcessor
    {
        public OperandShortInlineBrTargetProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            int pos = Info.Reader.ReadByte();
            pos += Info.Reader.Position;
            Builder.AppendFormat("{0:X4}", pos);
        }
    }

    public class OperandInlineIProcessor : OperandProcessor
    {
        public OperandInlineIProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var inlineI = Info.Reader.ReadInt();
            Builder.Append(inlineI);
        }
    }

    public class OperandInlineI8Processor : OperandProcessor
    {
        public OperandInlineI8Processor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var inlineI8 = Info.Reader.ReadLong();
            Builder.Append(inlineI8);
        }
    }

    public class OperandShortInlineRProcessor : OperandProcessor
    {
        public OperandShortInlineRProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var inlineF = Info.Reader.ReadFloat();
            Builder.Append(inlineF);
        }
    }

    public class OperandInlineRProcessor : OperandProcessor
    {
        public OperandInlineRProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var inlineR = Info.Reader.ReadDouble();
            Builder.Append(inlineR);
        }
    }

    public class OperandInlineTypeProcessor : OperandProcessor
    {
        public OperandInlineTypeProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            Info.AppendType(Builder);
        }
    }

    public class OperandShortInlineVarProcessor : OperandProcessor
    {
        public OperandShortInlineVarProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var _byte = Info.Reader.ReadByte();
            Builder.Append("[").Append(_byte).Append("]");
        }
    }

    public class OperandInlineFieldProcessor : OperandProcessor
    {
        public OperandInlineFieldProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            Info.AppendField(Builder);
        }
    }

    public class OperandInlineBrTargetProcessor : OperandProcessor
    {
        public OperandInlineBrTargetProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var pos = Info.Reader.ReadInt();
            pos += Info.Reader.Position;
            Builder.Append($"{pos:X4}");
        }
    }

    public class OperandInlineSigProcessor : OperandProcessor
    {
        public OperandInlineSigProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            Info.AppendSignature(Builder);
        }
    }

    public class OperandInlineSwitchProcessor : OperandProcessor
    {
        public OperandInlineSwitchProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var n = Info.Reader.ReadInt();
            var poss = new int[n];
            for (int i = 0; i < n; i++)
            {
                poss[i] = Info.Reader.ReadInt();
            }
            for (int i = 0; i < n; i++)
            {
                poss[i] += Info.Reader.Position;
            }
            Builder.Append("(");
            foreach (var p in poss)
            {
                Builder.Append($"{p:X4},");
            }
            Builder.TrimEnd(",");
            Builder.Append(")");
        }
    }

    public class OperandInlineTokProcessor : OperandProcessor
    {
        public OperandInlineTokProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            Info.AppendMember(Builder);
        }
    }

    public class OperandInlineVarProcessor : OperandProcessor
    {
        public OperandInlineVarProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var inlineVar = Info.Reader.ReadShort();
            Builder.Append(inlineVar);
        }
    }

    public class OperandShortInlineIProcessor : OperandProcessor
    {
        public OperandShortInlineIProcessor(StringBuilder builder, OpInfo info) : base(builder, info)
        {
        }

        public override void ReadOperand()
        {
            var _byte = Info.Reader.ReadByte();
            Builder.Append(_byte);
        }
    }
}
