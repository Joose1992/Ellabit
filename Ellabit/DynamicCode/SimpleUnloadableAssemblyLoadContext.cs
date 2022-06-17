﻿using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Ellabit.Challenges;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Ellabit.DynamicCode
{
    public class SimpleUnloadableAssemblyLoadContext : AssemblyLoadContext, IDisposable
    {
        HttpClient client;
        public SimpleUnloadableAssemblyLoadContext(HttpClient httpClient)
           : base(isCollectible: true)
        {
            client = httpClient;
        }
        public IChallenge? Challenge { get; set; }

        public void Dispose()
        {
            client = null;
            Challenge = null;
        }

        public async Task<(bool pass, string message)> RunTest(string test)
        {

            var assembly = await GetAssembly();

            Type? twriter = assembly?.GetType("Ellabit.TestChallenge");
            if (twriter == null)
            {
                throw new InvalidCastException("Ellabit.TestChallenge");
            }
            MethodInfo? method = twriter.GetMethod(test);
            if (method == null)
            {
                throw new InvalidCastException(test ?? "Empty Method");
            }
            var writer = Activator.CreateInstance(twriter);

            var output = method?.Invoke(writer, new object[] { });
            writer = null;
            if (output == null)
            {
                throw new InvalidDataException("Output is null");
            }
            if (!(output is (bool pass, string message)))
            {
                throw new InvalidDataException("Output is not the correct type");
            }
            return ((bool pass, string message))output;
        }
        private async Task<Assembly?> GetAssembly()
        {
            Assembly? assembly = null;
            try
            {
                assembly = this.Assemblies.FirstOrDefault(asm => asm.FullName ==  "EllabitChallenge, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            }
            catch { }
            if (assembly != null)
            {
                return assembly;
            }
            if (Challenge == null)
            {
                return null;
            }
            SyntaxTree codeChallengeTree = CSharpSyntaxTree.ParseText(Challenge.Code ?? "");
            SyntaxTree codeTestTree = CSharpSyntaxTree.ParseText(Challenge.TestCode ?? "");

            string assemblyName = Path.GetFileName("EllabitChallenge");

            Assembly[] referenceAssemblyRoots = new[]
            {
                typeof(AssemblyTargetedPatchBandAttribute).Assembly, // System.Private.CoreLib
                typeof(Uri).Assembly, // System.Private.Uri
                typeof(Console).Assembly, // System.Console
                typeof(IQueryable).Assembly, // System.Linq.Expressions
                typeof(HttpClient).Assembly, // System.Net.Http
                typeof(RequiredAttribute).Assembly, // System.ComponentModel.Annotations
                typeof(Regex).Assembly, // System.Text.RegularExpressions
                typeof(NavLink).Assembly, // Microsoft.AspNetCore.Components.Web
                typeof(WebAssemblyHostBuilder).Assembly, // Microsoft.AspNetCore.Components.WebAssembly
            };

            List<string> referenceAssemblyNames = referenceAssemblyRoots
                .SelectMany(a => a.GetReferencedAssemblies().Concat(new[] { a.GetName() }))
                .Select(an => an.Name ?? "")
                .ToList();
            if (referenceAssemblyNames == null)
            {
                return null;
            }

            var referenceAssembliesStreams = await this.GetReferenceAssembliesStreamsAsync(referenceAssemblyNames);

            var references = referenceAssembliesStreams
                .Select(s => MetadataReference.CreateFromStream(s, MetadataReferenceProperties.Assembly))
                .ToList();


            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { codeChallengeTree, codeTestTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    string error = string.Empty;
                    foreach (Diagnostic diagnostic in failures)
                    {
                        error += $"{diagnostic.Id} {diagnostic.GetMessage()}\n";
                    }
                    throw new Exception(error);
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = this.LoadFromStream(ms);
                }
            }
            return assembly;
        }


        private async Task<IEnumerable<Stream>> GetReferenceAssembliesStreamsAsync(IEnumerable<string> referenceAssemblyNames)
        {
            var streams = new ConcurrentBag<Stream>();

            await Task.WhenAll(
                referenceAssemblyNames.Select(async assemblyName =>
                {
                    var result = await client.GetAsync($"/_framework/{assemblyName}.dll");

                    result.EnsureSuccessStatusCode();

                    streams.Add(await result.Content.ReadAsStreamAsync());
                }));

            return streams;
        }
    }

}
