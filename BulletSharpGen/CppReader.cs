﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangSharp;

namespace BulletSharpGen
{
    class ReaderContext
    {
        public TranslationUnit TranslationUnit { get; set; }
        public HeaderDefinition Header { get; set; }
        public string Namespace { get; set; }
        public ClassDefinition Class { get; set; }
        public MethodDefinition Method { get; set; }
        public ParameterDefinition Parameter { get; set; }
        public FieldDefinition Field { get; set; }

        public AccessSpecifier MemberAccess { get; set; }
    }

    class CppReader
    {
        string src;
        Index index;
        List<string> headerQueue = new List<string>();
        List<string> clangOptions = new List<string>();
        Dictionary<string, string> excludedMethods = new Dictionary<string, string>();

        ReaderContext _context = new ReaderContext();
        WrapperProject project;

        public CppReader(WrapperProject project)
        {
            this.project = project;

            string sourceDirectory = project.SourceRootFolders[0];
            src = Path.GetFullPath(sourceDirectory);
            src = src.Replace('\\', '/');

            string[] commonHeaders;
            List<string> excludedHeaders = new List<string>();

            // Exclude C API
            excludedHeaders.Add(src + "Bullet-C-Api.h");

            // Include directory
            clangOptions.Add("-I");
            clangOptions.Add(src);

            // WorldImporter include directory
            clangOptions.Add("-I");
            clangOptions.Add(src + "../Extras/Serialize/BulletWorldImporter");

            // Specify C++ headers, not C ones
            clangOptions.Add("-x");
            clangOptions.Add("c++-header");

            //clangOptions.Add("-DUSE_DOUBLE_PRECISION");

            // Exclude irrelevant methods
            excludedMethods.Add("operator new", null);
            excludedMethods.Add("operator delete", null);
            excludedMethods.Add("operator new[]", null);
            excludedMethods.Add("operator delete[]", null);
            excludedMethods.Add("operator+=", null);
            excludedMethods.Add("operator-=", null);
            excludedMethods.Add("operator*=", null);
            excludedMethods.Add("operator/=", null);
            excludedMethods.Add("operator==", null);
            excludedMethods.Add("operator!=", null);
            excludedMethods.Add("operator()", null);

            // Enumerate all header files in the source tree
            var headerFiles = Directory.EnumerateFiles(src, "*.h", SearchOption.AllDirectories);
            foreach (string header in headerFiles)
            {
                if (header.Contains("GpuSoftBodySolvers") || header.Contains("vectormath"))
                {
                    continue;
                }

                string headerCanonical = header.Replace('\\', '/');
                if (!excludedHeaders.Contains(headerCanonical))
                {
                    headerQueue.Add(headerCanonical);
                }
            }

            Console.Write("Reading headers");

            index = new Index();

            // Parse the common headers
            commonHeaders = new[] { src + "btBulletCollisionCommon.h", src + "btBulletDynamicsCommon.h" };
            foreach (string commonHeader in commonHeaders)
            {
                if (!headerQueue.Contains(commonHeader))
                {
                    Console.WriteLine("Could not find " + commonHeader);
                    return;
                }
                ReadHeader(commonHeader);
            }

            while (headerQueue.Count != 0)
            {
                ReadHeader(headerQueue[0]);
            }

            if (Directory.Exists(src + "..\\Extras\\"))
            {
                ReadHeader(src + "..\\Extras\\Serialize\\BulletFileLoader\\btBulletFile.h");
                ReadHeader(src + "..\\Extras\\Serialize\\BulletWorldImporter\\btBulletWorldImporter.h");
                ReadHeader(src + "..\\Extras\\Serialize\\BulletWorldImporter\\btWorldImporter.h");
                ReadHeader(src + "..\\Extras\\Serialize\\BulletXmlWorldImporter\\btBulletXmlWorldImporter.h");
                ReadHeader(src + "..\\Extras\\HACD\\hacdHACD.h");
            }

            index.Dispose();

            Console.WriteLine();
            Console.WriteLine("Read complete - headers: {0}, classes: {1}",
                project.HeaderDefinitions.Count, project.ClassDefinitions.Count);


            foreach (var @class in project.ClassDefinitions.Values)
            {
                if (!@class.IsParsed)
                {
                    Console.WriteLine("Class removed: {0}", @class.FullyQualifiedName);
                }
            }
        }

        Cursor.ChildVisitResult HeaderVisitor(Cursor cursor, Cursor parent)
        {
            string filename = cursor.Extent.Start.File.Name.Replace('\\', '/');
            if (!filename.StartsWith(src, StringComparison.OrdinalIgnoreCase))
            {
                return Cursor.ChildVisitResult.Continue;
            }

            // Have we visited this header already?
            if (project.HeaderDefinitions.ContainsKey(filename))
            {
                _context.Header = project.HeaderDefinitions[filename];
            }
            else
            {
                // No, define a new one
                string relativeFilename = filename.Substring(src.Length);
                _context.Header = new HeaderDefinition(relativeFilename);
                project.HeaderDefinitions.Add(filename, _context.Header);
                headerQueue.Remove(filename);
            }

            if ((cursor.Kind == CursorKind.ClassDecl || cursor.Kind == CursorKind.StructDecl ||
                cursor.Kind == CursorKind.ClassTemplate || cursor.Kind == CursorKind.TypedefDecl ||
                cursor.Kind == CursorKind.EnumDecl) && cursor.IsDefinition)
            {
                ParseClassCursor(cursor);
            }
            else if (cursor.Kind == CursorKind.Namespace)
            {
                _context.Namespace = cursor.Spelling;
                return Cursor.ChildVisitResult.Recurse;
            }
            return Cursor.ChildVisitResult.Continue;
        }

        Cursor.ChildVisitResult EnumVisitor(Cursor cursor, Cursor parent)
        {
            if (cursor.Kind == CursorKind.EnumConstantDecl)
            {
                var @enum = _context.Class as EnumDefinition;
                @enum.EnumConstants.Add(cursor.Spelling);
                @enum.EnumConstantValues.Add("");
            }
            else if (cursor.Kind == CursorKind.IntegerLiteral)
            {
                var @enum = _context.Class as EnumDefinition;
                Token intLiteralToken = _context.TranslationUnit.Tokenize(cursor.Extent).First();
                @enum.EnumConstantValues[@enum.EnumConstants.Count - 1] = intLiteralToken.Spelling;
            }
            else if (cursor.Kind == CursorKind.ParenExpr)
            {
                return Cursor.ChildVisitResult.Continue;
            }
            return Cursor.ChildVisitResult.Recurse;
        }

        void ParseClassCursor(Cursor cursor)
        {
            string className = cursor.Spelling;

            // Unnamed struct
            // A combined "typedef struct {}" definition is split into separate struct and typedef statements
            // where the struct is also a child of the typedef, so the struct can be skipped for now.
            if (string.IsNullOrEmpty(className) && cursor.Kind == CursorKind.StructDecl)
            {
                return;
            }

            string fullyQualifiedName = TypeRefDefinition.GetFullyQualifiedName(cursor);
            if (project.ClassDefinitions.ContainsKey(fullyQualifiedName))
            {
                if (project.ClassDefinitions[fullyQualifiedName].IsParsed)
                {
                    return;
                }
                var parent = _context.Class;
                _context.Class = project.ClassDefinitions[fullyQualifiedName];
                _context.Class.Parent = parent;
            }
            else
            {
                if (cursor.Kind == CursorKind.ClassTemplate)
                {
                    _context.Class = new ClassTemplateDefinition(className, _context.Header, _context.Class);
                }
                else if (cursor.Kind == CursorKind.EnumDecl)
                {
                    _context.Class = new EnumDefinition(className, _context.Header, _context.Class);
                }
                else
                {
                    _context.Class = new ClassDefinition(className, _context.Header, _context.Class);
                }

                _context.Class.NamespaceName = _context.Namespace;

                if (_context.Class.FullyQualifiedName != fullyQualifiedName)
                {
                    // TODO
                }
                project.ClassDefinitions.Add(fullyQualifiedName, _context.Class);
            }

            _context.Class.IsParsed = true;

            // Unnamed struct escapes to the surrounding scope
            if (!(string.IsNullOrEmpty(className) && cursor.Kind == CursorKind.StructDecl))
            {
                if (_context.Class.Parent != null)
                {
                    _context.Class.Parent.Classes.Add(_context.Class);
                }
                else
                {
                    _context.Header.Classes.Add(_context.Class);
                }
            }

            AccessSpecifier parentMemberAccess = _context.MemberAccess;

            // Default class/struct access specifier
            if (cursor.Kind == CursorKind.ClassDecl)
            {
                _context.MemberAccess = AccessSpecifier.Private;
            }
            else if (cursor.Kind == CursorKind.StructDecl)
            {
                _context.Class.IsStruct = true;
                _context.MemberAccess = AccessSpecifier.Public;
            }
            else if (cursor.Kind == CursorKind.ClassTemplate)
            {
                if (cursor.TemplateCursorKind != CursorKind.ClassDecl)
                {
                    _context.MemberAccess = AccessSpecifier.Private;
                }
                else
                {
                    _context.MemberAccess = AccessSpecifier.Public;
                }
            }

            if (cursor.Kind == CursorKind.EnumDecl)
            {
                cursor.VisitChildren(EnumVisitor);
            }
            else if (cursor.Kind == CursorKind.TypedefDecl)
            {
                _context.Class.IsTypedef = true;
                if (cursor.TypedefDeclUnderlyingType.Canonical.TypeKind != ClangSharp.Type.Kind.FunctionProto)
                {
                    _context.Class.TypedefUnderlyingType = new TypeRefDefinition(cursor.TypedefDeclUnderlyingType);
                }
            }
            else
            {
                cursor.VisitChildren(ClassVisitor);
            }

            // Restore parent state
            _context.Class = _context.Class.Parent;
            _context.MemberAccess = parentMemberAccess;
        }

        Cursor.ChildVisitResult MethodTemplateTypeVisitor(Cursor cursor, Cursor parent)
        {
            if (cursor.Kind == CursorKind.TypeRef)
            {
                if (cursor.Referenced.Kind == CursorKind.TemplateTypeParameter)
                {
                    if (_context.Parameter != null)
                    {
                        _context.Parameter.Type.HasTemplateTypeParameter = true;
                    }
                    else
                    {
                        _context.Method.ReturnType.HasTemplateTypeParameter = true;
                    }
                    return Cursor.ChildVisitResult.Break;
                }
            }
            else if (cursor.Kind == CursorKind.TemplateRef)
            {
                // TODO
                return Cursor.ChildVisitResult.Recurse;
            }
            return Cursor.ChildVisitResult.Continue;
        }

        Cursor.ChildVisitResult FieldTemplateTypeVisitor(Cursor cursor, Cursor parent)
        {
            switch (cursor.Kind)
            {
                case CursorKind.TypeRef:
                    if (cursor.Referenced.Kind == CursorKind.TemplateTypeParameter)
                    {
                        _context.Field.Type.HasTemplateTypeParameter = true;
                    }
                    return Cursor.ChildVisitResult.Break;

                case CursorKind.TemplateRef:
                    if (parent.Type.Declaration.Kind == CursorKind.ClassTemplate)
                    {
                        _context.Field.Type.HasTemplateTypeParameter = true;
                        return Cursor.ChildVisitResult.Break;
                    }
                    return Cursor.ChildVisitResult.Continue;

                default:
                    return Cursor.ChildVisitResult.Continue;
            }
        }

        Cursor.ChildVisitResult ClassVisitor(Cursor cursor, Cursor parent)
        {
            switch (cursor.Kind)
            {
                case CursorKind.CxxAccessSpecifier:
                    _context.MemberAccess = cursor.AccessSpecifier;
                    return Cursor.ChildVisitResult.Continue;
                case CursorKind.CxxBaseSpecifier:
                    string baseName = TypeRefDefinition.GetFullyQualifiedName(cursor.Type);
                    ClassDefinition baseClass;
                    if (!project.ClassDefinitions.TryGetValue(baseName, out baseClass))
                    {
                        Console.WriteLine("Base {0} for {1} not found! Missing header?", baseName, _context.Class.Name);
                        return Cursor.ChildVisitResult.Continue;
                    }
                    _context.Class.BaseClass = baseClass;
                    return Cursor.ChildVisitResult.Continue;
                case CursorKind.TemplateTypeParameter:
                    var classTemplate = _context.Class as ClassTemplateDefinition;
                    if (classTemplate.TemplateTypeParameters == null)
                    {
                        classTemplate.TemplateTypeParameters = new List<string>();
                    }
                    classTemplate.TemplateTypeParameters.Add(cursor.Spelling);
                    return Cursor.ChildVisitResult.Continue;
            }

            if (_context.MemberAccess != AccessSpecifier.Public)
            {
                return Cursor.ChildVisitResult.Continue;
            }

            if ((cursor.Kind == CursorKind.ClassDecl || cursor.Kind == CursorKind.StructDecl ||
                cursor.Kind == CursorKind.ClassTemplate || cursor.Kind == CursorKind.TypedefDecl ||
                cursor.Kind == CursorKind.EnumDecl) && cursor.IsDefinition)
            {
                ParseClassCursor(cursor);
            }
            else if (cursor.Kind == CursorKind.CxxMethod || cursor.Kind == CursorKind.Constructor)
            {
                string methodName = cursor.Spelling;
                if (excludedMethods.ContainsKey(methodName))
                {
                    return Cursor.ChildVisitResult.Continue;
                }

                _context.Method = new MethodDefinition(methodName, _context.Class, cursor.NumArguments)
                {
                    ReturnType = new TypeRefDefinition(cursor.ResultType),
                    IsStatic = cursor.IsStaticCxxMethod,
                    IsConstructor = cursor.Kind == CursorKind.Constructor
                };

                if (cursor.IsVirtualCxxMethod)
                {
                    _context.Method.IsVirtual = true;
                    if (cursor.IsPureVirtualCxxMethod)
                    {
                        _context.Method.IsAbstract = true;
                        _context.Class.IsAbstract = true;
                    }
                }

                // Check if the return type is a template
                cursor.VisitChildren(MethodTemplateTypeVisitor);

                // Parse arguments
                for (uint i = 0; i < cursor.NumArguments; i++)
                {
                    Cursor arg = cursor.GetArgument(i);

                    string parameterName = arg.Spelling;
                    if (parameterName.Length == 0)
                    {
                        parameterName = "__unnamed" + i;
                    }
                    _context.Parameter = new ParameterDefinition(parameterName, new TypeRefDefinition(arg.Type));
                    _context.Method.Parameters[i] = _context.Parameter;
                    arg.VisitChildren(MethodTemplateTypeVisitor);
                    _context.Parameter = null;

                    // Check if it's a const or optional parameter
                    IEnumerable<Token> argTokens = _context.TranslationUnit.Tokenize(arg.Extent);
                    foreach (Token token in argTokens)
                    {
                        if (token.Spelling.Equals("="))
                        {
                            _context.Method.Parameters[i].IsOptional = true;
                        }
                    }
                }

                _context.Method = null;
            }
            else if (cursor.Kind == CursorKind.FieldDecl)
            {
                _context.Field = new FieldDefinition(cursor.Spelling,
                    new TypeRefDefinition(cursor.Type), _context.Class);
                if (!cursor.Type.Declaration.SpecializedCursorTemplate.IsInvalid)
                {
                    if (cursor.Children[0].Kind != CursorKind.TemplateRef)
                    {
                        throw new InvalidOperationException();
                    }
                    if (cursor.Children.Count == 1)
                    {
                        string displayName = cursor.Type.Declaration.DisplayName;
                        int typeStart = displayName.IndexOf('<') + 1;
                        int typeEnd = displayName.LastIndexOf('>');
                        displayName = displayName.Substring(typeStart, typeEnd - typeStart);
                        var specializationTypeRef = new TypeRefDefinition()
                        {
                            IsBasic = true,
                            Name = displayName
                        };
                        _context.Field.Type.SpecializedTemplateType = specializationTypeRef;
                    }
                    if (cursor.Children.Count == 2)
                    {
                        if (cursor.Children[1].Type.TypeKind != ClangSharp.Type.Kind.Invalid)
                        {
                            _context.Field.Type.SpecializedTemplateType = new TypeRefDefinition(cursor.Children[1].Type);
                        }
                        else
                        {
                            // TODO
                        }
                    }
                }
                //cursor.VisitChildren(FieldTemplateTypeVisitor);
                _context.Field = null;
            }
            else if (cursor.Kind == CursorKind.UnionDecl)
            {
                return Cursor.ChildVisitResult.Recurse;
            }
            else
            {
                //Console.WriteLine(cursor.Spelling);
            }
            return Cursor.ChildVisitResult.Continue;
        }

        void ReadHeader(string headerFile)
        {
            Console.Write('.');

            var unsavedFiles = new UnsavedFile[] { };
            using (_context.TranslationUnit = index.CreateTranslationUnit(headerFile, clangOptions.ToArray(), unsavedFiles, TranslationUnitFlags.SkipFunctionBodies))
            {
                var cur = _context.TranslationUnit.Cursor;
                _context.Namespace = "";
                cur.VisitChildren(HeaderVisitor);
            }
            _context.TranslationUnit = null;
            headerQueue.Remove(headerFile);
        }
    }
}
