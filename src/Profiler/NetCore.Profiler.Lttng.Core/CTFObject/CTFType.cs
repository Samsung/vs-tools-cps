/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    class CTFIntType : CTFType
    {
        int size;
        int talign;
        bool signed = true;
        #pragma warning disable 0414
        int encoding; // 0 - none
        #pragma warning restore 0414
        int ibase = 10;

        public CTFIntType(List<CTFAssignmentExpression> cae)
        {
            foreach (CTFAssignmentExpression ae in cae)
            {
                string name = ae.GetName();
                CTFUnaryExpression src = ae.Src;
                switch (name)
                {
                    case "size":
                        size = src.Calculate();
                        break;
                    case "align":
                        talign = src.Calculate();
                        break;
                    case "base":
                        ibase = src.Calculate();
                        break;
                    case "encoding":
                        string value = src.GetValue().GetString();
                        switch (value)
                        {
                            case "none":
                                encoding = 0;
                                break;
                            case "UTF8":
                                encoding = 1;
                                break;
                        }

                        break;
                    case "signed":
                        string v = src.GetValue().GetString();
                        switch (v)
                        {
                            case "false":
                                signed = false;
                                break;
                            case "0":
                                signed = false;
                                break;
                            case "1":
                                signed = true;
                                break;
                            case "true":
                                signed = true;
                                break;
                            default:
                                throw new CTFException();
                        }

                        break;
                    case "map": // TODO
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }

        internal override object GetObject(BitReader r)
        {
            return r.ReadIntObject(signed, talign, size);
        }

        internal override int Align()
        {
            if (talign != 0)
            {
                return talign;
            }

            return size;
        }
    }

    class CTFEnumType : CTFType
    {

        private CTFEnumElem[] elems;
        CTFType ctfType;

        public CTFEnumType(CTFScope scope, CTFEnumSpecifier ces) 
        {
            ctfType = GetType(scope, ces.Cds);
            elems = ces.List.ToArray();
        }

        internal override object GetObject(BitReader r)
        {
            return ctfType.GetObject(r);
        }

        internal string GetName(int id)
        {
            foreach (CTFEnumElem e in elems)
            {
                if (id >= e.First && id <= e.Second)
                {
                    return e.Name;
                }
            }

            throw new CTFException();
        }

        internal override int Align()
        {
            return ctfType.Align();
        }
    }

    class CTFStringType : CTFType // must be unique
    {
        public CTFStringType()
        {
        }

        internal override object GetObject(BitReader r)
        {
            StringBuilder sb = new StringBuilder(64);
            for (;;)
            {
                int c = r.ReadChar();
                if (c == 0)
                {
                    break;
                }

                sb.Append((char)c);
            }

            return sb.ToString();
        }
    }

 
    class CTFSElem // Struct element can be simple type, static and dyncmic array
    {
        public string Name { get; private set; }
        public CTFType CtfType { get; private set; }
        public CTFSElem(string name, CTFType ctfType)
        {
            this.Name = name;
            this.CtfType = ctfType;
        }

        internal virtual object Read(CTFStructType t, Object[] objects, BitReader r)
        {
            return CtfType.GetObject(r);
        }

        protected object ReadArray(BitReader r, uint len)
        {
            object[] array = new object[len];
            for (int i = 0; i < len; i++)
            {
                array[i] = CtfType.GetObject(r);
            }

            return array;
        }
    }

    class CTFSSElem : CTFSElem
    {
        int length;

        public CTFSSElem(string name, CTFType ctfType, int length) : base(name, ctfType) 
        {
            this.length = length;
        }

        internal override object Read(CTFStructType t, Object[] objects, BitReader r)
        {
            return ReadArray(r, (uint)length);
        }
    }

    class CTFDSElem : CTFSElem
    {
        int pos;

        public CTFDSElem(string name, CTFType ctfType, int pos) : base(name, ctfType)
        {
            this.pos = pos;
        }

        internal override object Read(CTFStructType t, Object[] objects, BitReader r)
        {
            return ReadArray(r, (uint)objects[pos]);
        }
    }

    class CTFVariantStructType : CTFStructType
    {
        private Dictionary<string, CTFStructType> subtypes;

        public CTFVariantStructType(Dictionary<string, CTFStructType> subtypes, List<CTFSElem> lelems, int align)
        {
            this.subtypes = subtypes;
            Elems = lelems.ToArray<CTFSElem>();
            talign = align;
        }

        public override CTFRecord Read(BitReader r)
        {
            r.Align(Align());
            uint id = (uint)Elems[0].Read(this, null, r); // must be int
            CTFEnumType ct = (CTFEnumType)Elems[0].CtfType; // must be enum

            // Select correct range
            string name = ct.GetName((int)id);
            CTFStructType cst = subtypes[name];
            if (cst.Changed)
            {
                return cst.Read(r);
            }
            else
            {
                CTFRecord cr = cst.Read(r, 1);
                cr.array[0] = id;
                return cr;
            }
        }
    }

    class CTFStructType : CTFType
    {
        protected int talign;
        protected int count;
        public CTFSElem[] Elems;
        public bool Changed { get; private set; }

        public CTFStructType()
        {
        }

        public CTFStructType(List<CTFSElem> lelems, int align, bool changed = false) 
        {
            Elems = lelems.ToArray<CTFSElem>();
            talign = align;
            count = Elems.Length;
            this.Changed = changed;
        }

        private static bool FindName(string name, List<CTFSElem> lelems, out int pos)
        {
            for (pos = 0; pos < lelems.Count; pos++)
            {
                if (lelems[pos].Name == name)
                {
                    return true;
                }
            }

            return false;
        }

        public static CTFStructType GetStructType(CTFScope scope, List<CTFStructOrVariantDeclaration> list, int align) 
        {
            if (list == null)
            {
                return new CTFStructType();
            }

            int count = list.Count; // Is it OK ?
            List<CTFSElem> lelems = new List<CTFSElem>();
            Dictionary<string, CTFStructType> subtypes = new Dictionary<string, CTFStructType>();
            bool hasVariants = false;

            foreach (CTFStructOrVariantDeclaration csvd in list)
            {
                CTFType ctfType = GetType(scope, csvd.List);
                CTFDeclarator cd = csvd.Cd; // can be name or array, arry can have dynamic size
                if (cd.Cue != null)
                {
                    // It is array
                    if (cd.Cue.IsNumber())
                    {
                        lelems.Add(new CTFSSElem(cd.Name, ctfType, cd.Cue.Calculate()));
                    }
                    else // dynamic array with local variable size
                    {
                        int j;
                        if (!FindName(cd.Cue.GetName(), lelems, out j))
                        {
                            throw new CTFException();
                        }

                        lelems.Add(new CTFDSElem(cd.Name, ctfType, j));
                    }
                }
                else
                {
                    if (ctfType is CTFVariantType)
                    {
                        CTFVariantType variantType = (CTFVariantType)ctfType;
                        hasVariants = true;
                        variantType.Process(subtypes, lelems, align);
                    }
                    else
                    {
                        lelems.Add(new CTFSElem(cd.Name, ctfType));
                    }
                }
            }

            return hasVariants ? new CTFVariantStructType(subtypes, lelems, align) : new CTFStructType(lelems, align);
        }

        internal override bool Fix(List<CTFSElem> lelems)
        {
            bool changed = false;
            foreach (CTFSElem e in Elems)
            {
                int i;
                if (FindName(e.Name, lelems, out i))
                {
                    lelems[i] = e; // upgrade
                    changed = true;
                }
                else
                {
                    lelems.Add(e);
                }
            }

            return changed;
        }

        internal CTFRecord Read(BitReader r, int add)
        {
            Object[] objects = new Object[count + add];
            for (int i = add; i < count; i++)
            {
                objects[i] = Elems[i].Read(this, objects, r);
            }

            return new CTFRecord(this, objects);
        }

        public override CTFRecord Read(BitReader r)
        {
            r.Align(Align());
            return Read(r, 0);
        }

        internal int GetPos(string name)
        {
            for (int i = 0; i < count; i++)
            {
                if (Elems[i].Name == name)
                {
                    return i;
                }
            }

            throw new CTFException();
        }

        internal override int Align()
        {
            if (talign != 0)
            {
                return talign;
            }

            //return 8; // defined by elements
            if (Elems == null || Elems.Length == 0)
            {
                return 8;
            }

            int nalign = 1;
            foreach (CTFSElem e in Elems)
            {
                int malign = e.CtfType.Align();
                if (malign > nalign)
                {
                    nalign = malign;
                }
            }

            talign = nalign;
            return nalign;
        }
    }

    public class CTFType
    {
        static public CTFType StringType = new CTFStringType();
        static public CTFType EmptyStruct = new CTFStructType();

        private static string FullName(List<CTFTypeSpecifier> list)
        {
            string name = "";
            foreach (CTFITypeSpecifier cts in list)
            {
                if (name != "")
                {
                    name += " ";
                }

                name += cts.GetName();
            }

            return name;
        }

        internal static void AddType(CTFScope scope, List<CTFTypeSpecifier> cds, List<CTFTypeSpecifier> cds2, CTFDeclarator cd)
        {
            // used by typealias
            CTFType ct = GetType(scope, cds);
            if (cds2 != null)
            {
                // Special case (unsigned long and so on)
                string name = FullName(cds2);
                CTFSymbol cs = new CTFSymbol(name, ct);
                scope.AddType(cs);
            }

            if (cd != null)
            {
                CTFSymbol cs = new CTFSymbol(cd.Name, ct);
                scope.AddType(cs);
            }
        }

        internal static CTFType GetStructType(CTFScope scope, string name, List<CTFStructOrVariantDeclaration> list, int align)
        {
            if (list == null)
            {
                // Already defined or empty ?
                return (name == null) ? EmptyStruct : scope.FindStructSymbol(name).CtfType;
            }

            CTFStructType cst = CTFStructType.GetStructType(scope, list, align);
            if (name != null)
            {
                scope.AddStruct(new CTFSymbol(name, cst));
            }

            return cst;
        }

        protected static CTFType GetType(CTFScope scope, List<CTFTypeSpecifier> cds)
        {
            // cases:
            // standard combinatons "unsigned long" - really as a special identifier
            // some kind of struct/variant/enum
            // integer/floating_point
            // string
            if (cds.Count == 1) // standard case
            {
                return cds[0].GetType(scope);
            }
            // Non trivial situation, something like "unsigned long"
            string name = FullName(cds);
            return scope.FindTypeSymbol(name).CtfType;
        }

        public virtual CTFRecord Read(BitReader r)
        {
            throw new NotImplementedException();
        }

        internal virtual object GetObject(BitReader r)
        {
            throw new NotImplementedException();
        }

        internal virtual bool Fix(List<CTFSElem> elems)
        {
            throw new NotImplementedException();
        }

        internal virtual int Align() => 8;

    }

    internal class CTFFloatType : CTFType
    {
        int exp_dig;
        int mant_dig;
        int talign;

        public CTFFloatType(List<CTFAssignmentExpression> cae) 
        {
            foreach (CTFAssignmentExpression ae in cae)
            {
                //CTFUnaryExpression cue = ae.dst; // It must be identifier
                string name = ae.GetName();
                CTFUnaryExpression src = ae.Src;
                switch (name)
                {
                    case "exp_dig":
                        exp_dig = src.Calculate();
                        break;
                    case "mant_dig":
                        mant_dig = src.Calculate();
                        break;
                    case "align":
                        talign = src.Calculate();
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }

        internal override object GetObject(BitReader r)
        {
            r.ReadIntObject(false, talign, talign);
            return (object)0.0;
        }

    }

    class CTFVElem
    {
        private CTFType ct;
        public string Name { get; private set; }

        public CTFVElem(string name, CTFType ct)
        {
            this.Name = name;
            this.ct = ct;
        }

        public CTFStructType GetStructType(List<CTFSElem> lelems)
        {
            List<CTFSElem> nelems = new List<CTFSElem>(lelems);
            bool changed = ct.Fix(nelems);
            return new CTFStructType(nelems, 0, changed);
        }
    }

    internal class CTFVariantType : CTFType
    {
        string tag; // tag name
        CTFVElem[] elems;

        public CTFVariantType(CTFScope scope, CTFVaraintSpecifier vs) 
        {
            tag = vs.Cue.GetName();
            elems = new CTFVElem[vs.List.Count];
            int i = 0;
            foreach (CTFStructOrVariantDeclaration cd in vs.List)
            {
                CTFType ct = GetType(scope, cd.List);
                string name = cd.Cd.Name;
                elems[i++] = new CTFVElem(name, ct);
            }
        }

        internal void Process(Dictionary<string, CTFStructType> subtypes, List<CTFSElem> lelems, int align)
        {
            foreach (CTFVElem cve in elems)
            {
                subtypes.Add(cve.Name, cve.GetStructType(lelems));
            }
        }
    }
}
